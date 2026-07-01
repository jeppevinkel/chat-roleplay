using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatRoleplay.Config;
using ChatRoleplay.Models;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

// ─── OpenAI / Ollama request & response DTOs ────────────────────────────────

internal class OpenAiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; init; } = [];

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolDefinition>? Tools { get; init; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolChoice { get; init; }
}

internal class OllamaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; init; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolDefinition>? Tools { get; init; }
}

internal class ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; init; } = new();
}

internal class FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object Parameters { get; init; } = new();
}

internal class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; init; }
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; init; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; init; }
}

internal class ToolCall
{
    [JsonPropertyName("function")]
    public FunctionCall? Function { get; init; }
}

internal class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;
}

internal class OllamaResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; init; }
}

internal class OllamaMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; init; }
}

// ─── AiService ──────────────────────────────────────────────────────────────

/// <summary>
/// Handles communication with OpenAI-compatible and Ollama AI providers.
/// Supports both tool/function calling mode and the legacy [CHAR]/[CONTENT] text format.
/// </summary>
public class AiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiService> _logger;
    private readonly CoreConfig _coreConfig;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AiService(
        IHttpClientFactory httpClientFactory,
        ILogger<AiService> logger,
        CoreConfig coreConfig)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _coreConfig = coreConfig;
    }

    /// <summary>
    /// Gets a completion from the configured AI provider.
    /// Returns a <see cref="CharacterResponse"/> with character name and message.
    /// </summary>
    public async Task<CharacterResponse?> GetCompletionAsync(
        List<ChatMessage> messages,
        List<Models.Character> characters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return _coreConfig.Provider.ToLower() switch
            {
                "openai" => await GetOpenAiCompletionAsync(messages, characters, cancellationToken),
                "ollama" => await GetOllamaCompletionAsync(messages, characters, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported provider: {_coreConfig.Provider}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI completion");
            return null;
        }
    }

    private async Task<CharacterResponse?> GetOpenAiCompletionAsync(
        List<ChatMessage> messages,
        List<Models.Character> characters,
        CancellationToken cancellationToken)
    {
        var client = CreateHttpClient("https://api.openai.com", _coreConfig.AiToken);

        if (_coreConfig.UseToolCalling)
        {
            var request = new OpenAiRequest
            {
                Model = _coreConfig.Model,
                Messages = messages,
                Tools = [BuildTool(characters)],
                ToolChoice = "required"
            };

            var response = await client.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, cancellationToken);
            var toolCall = result?.Choices?[0]?.Message?.ToolCalls?[0];

            if (toolCall?.Function?.Arguments != null)
            {
                return JsonSerializer.Deserialize<CharacterResponse>(toolCall.Function.Arguments, JsonOptions);
            }

            // Fallback to content if tool call missing
            var content = result?.Choices?[0]?.Message?.Content;
            return content != null ? ParseLegacyFormat(content) : null;
        }
        else
        {
            var request = new OpenAiRequest
            {
                Model = _coreConfig.Model,
                Messages = messages
            };

            var response = await client.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, cancellationToken);
            var content = result?.Choices?[0]?.Message?.Content;
            return content != null ? ParseLegacyFormat(content) : null;
        }
    }

    private async Task<CharacterResponse?> GetOllamaCompletionAsync(
        List<ChatMessage> messages,
        List<Models.Character> characters,
        CancellationToken cancellationToken)
    {
        var baseUrl = _coreConfig.CustomEndpoint ?? "http://localhost:11434";
        var client = CreateHttpClient(baseUrl, null);

        if (_coreConfig.UseToolCalling)
        {
            var request = new OllamaRequest
            {
                Model = _coreConfig.Model,
                Messages = messages,
                Stream = false,
                Tools = [BuildTool(characters)]
            };

            var response = await client.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions, cancellationToken);
            var toolCall = result?.Message?.ToolCalls?[0];

            if (toolCall?.Function?.Arguments != null)
            {
                return JsonSerializer.Deserialize<CharacterResponse>(toolCall.Function.Arguments, JsonOptions);
            }

            var content = result?.Message?.Content;
            return content != null ? ParseLegacyFormat(content) : null;
        }
        else
        {
            var request = new OllamaRequest
            {
                Model = _coreConfig.Model,
                Messages = messages,
                Stream = false
            };

            var response = await client.PostAsJsonAsync("/api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions, cancellationToken);
            var content = result?.Message?.Content;
            return content != null ? ParseLegacyFormat(content) : null;
        }
    }

    private HttpClient CreateHttpClient(string baseUrl, string? bearerToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        if (bearerToken != null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        return client;
    }

    private ToolDefinition BuildTool(List<Models.Character> characters)
    {
        return new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = "respond_as_character",
                Description = "Respond as one of the roleplay characters",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        character_name = new
                        {
                            type = "string",
                            @enum = characters.Select(c => c.Name).ToArray(),
                            description = "The character who is speaking"
                        },
                        message = new
                        {
                            type = "string",
                            description = "What the character says"
                        },
                        emotion = new
                        {
                            type = "string",
                            @enum = new[] { "happy", "sad", "excited", "nervous", "angry", "neutral" },
                            description = "The character's emotional state"
                        }
                    },
                    required = new[] { "character_name", "message" }
                }
            }
        };
    }

    /// <summary>
    /// Parses the legacy [CHAR] Name [CONTENT] Message format used by the Node.js version.
    /// </summary>
    private static CharacterResponse? ParseLegacyFormat(string text)
    {
        var charMatch = System.Text.RegularExpressions.Regex.Match(text, @"\[CHAR\](.*?)\[CONTENT\]");
        var contentMatch = System.Text.RegularExpressions.Regex.Match(text, @"\[CONTENT\](.*)");

        if (!charMatch.Success || !contentMatch.Success)
            return null;

        return new CharacterResponse
        {
            CharacterName = charMatch.Groups[1].Value.Trim(),
            Message = contentMatch.Groups[1].Value.Trim()
        };
    }
}
