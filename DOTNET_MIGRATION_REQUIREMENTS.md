# .NET 10 Migration Requirements for Chat-Roleplay

## Executive Summary
This document outlines the requirements to recreate the Chat-Roleplay Discord bot in .NET 10 (LTS). The current application is a TypeScript/Node.js project that creates multiple AI-powered Discord bot characters that can engage in roleplay conversations using Large Language Models (LLMs) like GPT-4 or local Ollama models.

## Current Application Overview

### Technology Stack (TypeScript/Node.js)
- **Runtime**: Node.js (v22+)
- **Language**: TypeScript 5.5.4
- **Discord Library**: discord.js v14.15.3
- **HTTP Client**: axios v1.7.4
- **Web Framework**: express v4.19.2 (minimal usage)

### Core Functionality
The application provides:
1. **Multi-Character AI Roleplay**: Multiple Discord bots (characters) that respond in roleplay scenarios
2. **AI Integration**: Support for OpenAI API and Ollama (local LLM)
3. **Idle Conversation**: Automated conversation continuation when users are inactive
4. **Configurable Characters**: JSON-based character configuration with personalities
5. **Channel Management**: Designated Discord channels for roleplay
6. **Character Selection**: Determines which character should respond (legacy: `[CHAR] Name [CONTENT] Message` format, modern: tool/function calling)

### Architecture (Current - Individual Bots)
```
Main Bot (Manager)
├── Listens to messages in roleplay channels
├── Manages AI prompts and responses
└── Coordinates multiple character bots

Character Bots (1-N)
├── Each has its own Discord bot token
├── Send messages as their character
└── Reply to messages in roleplay channels
```

**⚠️ NOTE**: This architecture uses individual bot accounts per character, which has significant limitations (see Section 18 for recommended webhook-based alternative).

## .NET 10 Migration Requirements

### 1. Target Framework & Runtime
- **.NET Version**: .NET 10 (LTS) - Released November 2026
- **Project Type**: Console Application or ASP.NET Core Worker Service
- **Language**: C# 13.0
- **Target Framework Moniker**: `net10.0`

### 2. Discord Library Selection

#### Recommended: DSharpPlus (v5.x or latest)
- **NuGet Package**: `DSharpPlus` 
- **Why**: Modern, actively maintained, excellent async/await support, supports .NET 10
- **Features Needed**:
  - Gateway intents for messages and guilds
  - Message creation/reply functionality
  - Multiple client instances (one per character)
  - Async event handling

#### Alternative: Discord.Net (v3.x or latest)
- **NuGet Package**: `Discord.Net`
- **Why**: Most popular Discord library for .NET, comprehensive documentation
- **Features Needed**:
  - Gateway intents configuration
  - Message handling
  - Multiple DiscordSocketClient instances

**Recommendation**: Use **DSharpPlus** for .NET 10 as it has better modern .NET support and a cleaner async/await implementation.

### 3. Required NuGet Packages

```xml
<ItemGroup>
  <!-- Discord Library -->
  <PackageReference Include="DSharpPlus" Version="5.*" />
  
  <!-- HTTP Client for AI API calls -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="10.*" />
  
  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.*" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.*" />
  
  <!-- Dependency Injection & Hosting -->
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.*" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
  
  <!-- Logging -->
  <PackageReference Include="Microsoft.Extensions.Logging" Version="10.*" />
  <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="10.*" />
  
  <!-- JSON Serialization -->
  <PackageReference Include="System.Text.Json" Version="10.*" />
</ItemGroup>
```

### 4. Project Structure

```
ChatRoleplay/
├── ChatRoleplay.csproj
├── Program.cs                          # Entry point
├── appsettings.json                    # Configuration
├── Models/
│   ├── Character.cs                    # Character model (with AvatarUrl, no BotToken)
│   ├── CoreConfig.cs                   # Core configuration model
│   ├── PromptConfig.cs                 # Prompt configuration model
│   ├── CharacterConfig.cs              # Character configuration model
│   ├── Message.cs                      # Message model
│   └── CharacterResponse.cs            # AI response model (tool calling)
├── Services/
│   ├── IConfigService.cs               # Configuration service interface
│   ├── ConfigService.cs                # Configuration service implementation
│   ├── IAiService.cs                   # AI service interface
│   ├── AiService.cs                    # AI service implementation (OpenAI/Ollama)
│   ├── WebhookService.cs               # Webhook management and character messaging
│   ├── ChannelService.cs               # Channel/conversation management
│   └── ManagerBotService.cs            # Main bot service
├── Providers/
│   ├── ILlmProvider.cs                 # LLM provider interface
│   ├── OpenAiProvider.cs               # OpenAI implementation with tool calling
│   └── OllamaProvider.cs               # Ollama implementation with tool calling
└── Data/                               # Runtime config storage
    ├── core-config.json
    ├── prompt-config.json
    └── character-config.json
```

### 5. Key Classes and Interfaces

#### 5.1 Models

**Character.cs**
```csharp
public record Character
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public string Personality { get; init; } = string.Empty;
    
    // For webhooks - no bot token needed!
    public string? AvatarUrl { get; init; }
}
```

**Message.cs**
```csharp
public record ChatMessage
{
    public string Role { get; init; } = string.Empty; // "system", "user", "assistant"
    public string Content { get; init; } = string.Empty;
}
```

**CoreConfig.cs**
```csharp
public record CoreConfig
{
    public ManagerBot ManagerBot { get; init; } = new();
    public string Model { get; init; } = "gpt-4o";
    public string Provider { get; init; } = "openai"; // "openai" or "ollama"
    public string? CustomEndpoint { get; init; }
    public string AiToken { get; init; } = string.Empty;
    public int IdleIntervalSec { get; init; } = 180;
    public List<ulong> RoleplayChannels { get; init; } = new();
}

public record ManagerBot
{
    public string BotToken { get; init; } = string.Empty;
}
```

#### 5.2 Services

**IAiService.cs**
```csharp
public interface IAiService
{
    Task<string?> GetCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default);
}
```

**ILlmProvider.cs**
```csharp
public interface ILlmProvider
{
    Task<string?> GetCompletionAsync(string model, List<ChatMessage> messages, CancellationToken cancellationToken = default);
}
```

**WebhookService.cs**
```csharp
public class WebhookService
{
    // Manages webhook creation and caching per channel
    // Sends messages as characters with custom name/avatar
    // No need for multiple Discord clients!
}
```

**ChannelService.cs**
```csharp
public class ChannelService
{
    // Manages conversation state for each roleplay channel
    // Handles message history/prompts
    // Implements idle conversation timeout
    // Parses [CHAR] Name [CONTENT] format
}
```

### 6. Configuration Management

#### Use .NET Configuration System
- **appsettings.json**: Default settings (not committed to git)
- **Data folder**: Runtime configuration files (JSON) created on first run
- **IConfiguration**: Standard .NET configuration interfaces
- **IOptions<T>**: Strongly-typed configuration

#### Configuration Loading Strategy
1. Create default configurations on first run if files don't exist
2. Load from JSON files in Data folder
3. Use `System.Text.Json` for serialization/deserialization
4. Support hot-reload capability (optional enhancement)

### 7. AI Provider Implementation

#### OpenAI Provider
- **Endpoint**: `https://api.openai.com/v1/chat/completions`
- **Authentication**: Bearer token in Authorization header
- **Request Format**: Standard OpenAI API format
```csharp
{
    "model": "gpt-4o",
    "messages": [
        { "role": "system", "content": "..." },
        { "role": "user", "content": "..." }
    ]
}
```

#### Ollama Provider
- **Endpoint**: `http://localhost:11434/api/chat` (default)
- **Authentication**: None required for local
- **Request Format**: Ollama-specific format
```csharp
{
    "model": "llama3.1",
    "messages": [...],
    "stream": false
}
```

#### Implementation Notes
- Use `IHttpClientFactory` for HTTP clients
- Implement retry logic with `Polly` library (optional but recommended)
- Handle rate limiting and timeouts
- Add logging for API calls

### 8. Discord Integration Requirements

#### Gateway Intents Required
```csharp
var intents = DiscordIntents.Guilds | 
              DiscordIntents.GuildMessages | 
              DiscordIntents.MessageContents;
```

**Note**: `MessageContents` is a privileged intent and must be enabled in Discord Developer Portal for each bot.

#### Multiple Client Instances
- **Manager Bot**: Listens to messages, coordinates responses
- **Character Bots**: Send messages as characters (1 client per character)
- Each needs separate login and event handling

#### Message Flow (Legacy Multi-Bot)
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service
4. Parse response for `[CHAR]` and `[CONTENT]`
5. Find matching character bot
6. Character bot replies to message

#### Message Flow (Recommended Webhook)
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service with tool calling
4. AI returns structured response: `{"character_name": "Monika", "message": "Hello!"}`
5. Look up character by name
6. Send via webhook with character's name and avatar

### 9. Asynchronous Programming

#### Key Patterns
- Use `async`/`await` throughout
- `Task` and `Task<T>` for async operations
- `CancellationToken` for graceful shutdown
- `IHostedService` or `BackgroundService` for long-running operations

#### Idle Conversation Implementation
```csharp
private async Task StartIdleTimerAsync(ulong channelId, CancellationToken cancellationToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.IdleIntervalSec));
    
    while (await timer.WaitForNextTickAsync(cancellationToken))
    {
        await SendIdleMessageAsync(channelId, cancellationToken);
    }
}
```

### 10. Logging and Monitoring

#### Use Microsoft.Extensions.Logging
```csharp
public class ChannelService
{
    private readonly ILogger<ChannelService> _logger;
    
    public ChannelService(ILogger<ChannelService> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleMessageAsync(DiscordMessage message)
    {
        _logger.LogInformation("Handling message from {User}: {Content}", 
            message.Author.Username, message.Content);
    }
}
```

#### Logging Levels
- **Debug**: AI prompts and responses
- **Information**: Message handling, bot startup/shutdown
- **Warning**: API errors, parsing failures
- **Error**: Critical failures, exceptions

### 11. Dependency Injection Setup

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configuration
        services.Configure<CoreConfig>(context.Configuration.GetSection("CoreConfig"));
        services.AddSingleton<IConfigService, ConfigService>();
        
        // HTTP clients
        services.AddHttpClient<ILlmProvider, OpenAiProvider>();
        services.AddHttpClient<ILlmProvider, OllamaProvider>();
        
        // Services
        services.AddSingleton<IAiService, AiService>();
        services.AddSingleton<WebhookService>();  // Webhook-based character messaging
        services.AddSingleton<ChannelService>();
        
        // Background services
        services.AddHostedService<ManagerBotService>();
    });
```

### 12. Docker Support

#### Dockerfile for .NET 10
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ChatRoleplay/ChatRoleplay.csproj", "ChatRoleplay/"]
RUN dotnet restore "ChatRoleplay/ChatRoleplay.csproj"

COPY . .
WORKDIR "/src/ChatRoleplay"
RUN dotnet build "ChatRoleplay.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ChatRoleplay.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directory
RUN mkdir -p /app/data

ENTRYPOINT ["dotnet", "ChatRoleplay.dll"]
```

#### Docker Compose
```yaml
version: "3.8"
services:
  chat-roleplay-dotnet:
    image: chat-roleplay-dotnet:latest
    restart: always
    volumes:
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

### 13. Error Handling and Resilience

#### Retry Logic
Use `Polly` for HTTP retry policies:
```csharp
services.AddHttpClient<ILlmProvider, OpenAiProvider>()
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

#### Exception Handling
- Wrap Discord event handlers in try-catch blocks
- Log exceptions with full context
- Graceful degradation (continue on character-specific errors)
- Health checks for critical dependencies

### 14. Testing Considerations

#### Unit Tests
- Test message parsing logic
- Test configuration loading
- Test AI response formatting
- Mock Discord API calls

#### Integration Tests
- Test AI provider integration
- Test Discord bot connectivity
- Test multi-client coordination

#### Testing Frameworks
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library

### 15. Performance Considerations

#### Memory Management
- Limit conversation history size (FIFO queue)
- Dispose Discord clients properly
- Use `Span<T>` and `Memory<T>` for string operations where applicable

#### Concurrency
- Handle multiple channels simultaneously
- Thread-safe collections for shared state
- `ConcurrentDictionary` for channel management

#### Caching
- Cache character configurations
- Cache prompt templates
- Consider memory cache for frequent lookups

### 16. Security Considerations

#### Token Management
- Never commit tokens to git (.gitignore data folder)
- Support environment variables for tokens
- Use User Secrets for development

#### API Key Storage
```csharp
// appsettings.json (not in git)
{
  "CoreConfig": {
    "ManagerBot": {
      "BotToken": "env:MANAGER_BOT_TOKEN"
    },
    "AiToken": "env:OPENAI_API_KEY"
  }
}
```

#### Input Validation
- Validate message content before sending to AI
- Sanitize user inputs
- Rate limiting on AI calls
- Content filtering for inappropriate responses

### 17. Character Selection Strategy: Legacy Format vs. Tool Calling

#### Current Implementation: `[CHAR] Name [CONTENT] Message`
The existing TypeScript implementation uses a custom text format where the AI is instructed via system prompt to respond in this specific format:
- `[CHAR] Monika [CONTENT] Hello everyone!`
- Requires regex parsing to extract character name and message content
- Works with any LLM but relies on prompt adherence
- Can be error-prone if the model doesn't follow the format strictly

#### Modern Approach: Tool/Function Calling (RECOMMENDED for .NET Migration)

**Why Tool Calling is Superior:**

1. **Native LLM Support**: Modern models (GPT-4, GPT-4o, Claude 3+, Llama 3.1+) have built-in tool/function calling
2. **Structured Output**: Guaranteed JSON schema adherence, no regex parsing needed
3. **Type Safety**: Direct deserialization to C# models
4. **Reliability**: LLMs are trained specifically for function calling, more consistent than prompt-based formatting
5. **Extensibility**: Easy to add more functions (e.g., character actions, emotions, scene changes)
6. **Better Error Handling**: API returns structured errors if function format is invalid

**Implementation Example:**

```csharp
// Define the function/tool for OpenAI API
public class CharacterResponseFunction
{
    public string Type { get; } = "function";
    public FunctionDefinition Function { get; set; }
}

public class FunctionDefinition
{
    public string Name { get; init; } = "respond_as_character";
    public string Description { get; init; } = "Respond as one of the roleplay characters";
    public JsonElement Parameters { get; init; } // JSON Schema
}

// JSON Schema for the function
{
    "type": "object",
    "properties": {
        "character_name": {
            "type": "string",
            "enum": ["Monika", "Sayori", "Yuri", "Natsuki"],
            "description": "The character who is speaking"
        },
        "message": {
            "type": "string",
            "description": "What the character says"
        },
        "emotion": {
            "type": "string",
            "enum": ["happy", "sad", "excited", "nervous", "angry", "neutral"],
            "description": "The character's emotional state (optional)"
        }
    },
    "required": ["character_name", "message"]
}
```

**OpenAI API Request with Tool Calling:**
```csharp
{
    "model": "gpt-4o",
    "messages": [...],
    "tools": [
        {
            "type": "function",
            "function": {
                "name": "respond_as_character",
                "description": "Respond as one of the roleplay characters",
                "parameters": { /* schema above */ }
            }
        }
    ],
    "tool_choice": "required" // Force the model to use the function
}
```

**API Response:**
```csharp
{
    "choices": [{
        "message": {
            "role": "assistant",
            "content": null,
            "tool_calls": [{
                "id": "call_abc123",
                "type": "function",
                "function": {
                    "name": "respond_as_character",
                    "arguments": "{\"character_name\":\"Monika\",\"message\":\"Hello everyone! Welcome to the Literature Club!\"}"
                }
            }]
        }
    }]
}
```

**Parsing in .NET:**
```csharp
public record CharacterResponse
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; init; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
    
    [JsonPropertyName("emotion")]
    public string? Emotion { get; init; }
}

// Simple deserialization
var response = JsonSerializer.Deserialize<CharacterResponse>(toolCall.Function.Arguments);
```

#### Comparison Table

| Aspect | Legacy `[CHAR]` Format | Tool/Function Calling |
|--------|----------------------|----------------------|
| **Parsing** | Regex, error-prone | Native JSON deserialization |
| **Reliability** | Depends on prompt adherence | Guaranteed schema compliance |
| **Type Safety** | String manipulation | Strongly-typed C# objects |
| **LLM Support** | All models | GPT-4+, Claude 3+, Llama 3.1+ |
| **Extensibility** | Requires prompt changes | Add new fields to schema |
| **Error Handling** | Try-catch on regex | Structured API errors |
| **Performance** | Slower (regex) | Faster (direct deserialize) |
| **Future-Proof** | Outdated approach | Industry standard |

#### Ollama Support for Tool Calling

Ollama (as of 2024+) supports tool/function calling for compatible models:
- Llama 3.1 and newer
- Mistral models
- Other recent models with function calling training

**Ollama API Request:**
```csharp
{
    "model": "llama3.1",
    "messages": [...],
    "tools": [ /* same format as OpenAI */ ],
    "stream": false
}
```

#### Recommendation for .NET Migration

**Use Tool/Function Calling as the primary implementation:**

1. **Default**: Implement tool calling for OpenAI and compatible Ollama models
2. **Fallback**: Keep legacy `[CHAR]` format as a fallback for older models
3. **Configuration**: Allow users to choose via config setting
4. **Provider Detection**: Auto-detect if model supports tool calling

**Implementation Strategy:**
```csharp
public interface ICharacterSelector
{
    Task<CharacterResponse> SelectCharacterAsync(List<ChatMessage> messages, CancellationToken cancellationToken);
}

public class ToolCallingCharacterSelector : ICharacterSelector
{
    // Modern approach - recommended
}

public class LegacyFormatCharacterSelector : ICharacterSelector
{
    // Fallback for older models
}

// In configuration
public record AiProviderConfig
{
    public bool UseToolCalling { get; init; } = true; // Default to modern approach
    public string Model { get; init; } = "gpt-4o";
}
```

#### Migration Decision

**STRONGLY RECOMMENDED**: Use tool/function calling for the .NET implementation unless:
- You need to support very old models (pre-2024)
- You're using a local model that doesn't support function calling
- You have a specific reason to preserve the exact legacy behavior

The tool calling approach is:
- More reliable
- More maintainable
- More extensible
- Better aligned with modern LLM best practices
- Better suited for .NET's strong typing

You can still support the legacy format as a fallback option, but the primary implementation should use tool calling.

#### Practical Implementation in .NET

**Models for Tool Calling:**
```csharp
// Request models
public record ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; init; } = new();
}

public record FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    [JsonPropertyName("parameters")]
    public object Parameters { get; init; } = new();
}

// Response models
public record ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; init; } = new();
}

public record FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;
}

// Character response model
public record CharacterResponse
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; init; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
    
    [JsonPropertyName("emotion")]
    public string? Emotion { get; init; }
}
```

**Service Implementation:**
```csharp
public class ToolCallingAiService : IAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ToolCallingAiService> _logger;
    private readonly List<Character> _characters;

    public async Task<CharacterResponse?> GetCharacterResponseAsync(
        List<ChatMessage> messages, 
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        
        // Build the tool definition
        var tool = new ToolDefinition
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
                            @enum = _characters.Select(c => c.Name).ToArray(),
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

        var request = new
        {
            model = "gpt-4o",
            messages = messages,
            tools = new[] { tool },
            tool_choice = "required" // Force model to use the function
        };

        var response = await client.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken);
        var toolCall = result?.Choices?[0]?.Message?.ToolCalls?[0];
        
        if (toolCall?.Function?.Arguments != null)
        {
            return JsonSerializer.Deserialize<CharacterResponse>(toolCall.Function.Arguments);
        }

        return null;
    }
}
```

**Benefits in Practice:**
1. **No Regex Parsing**: Direct JSON to C# object conversion
2. **Validation**: The model is constrained to valid character names via the enum
3. **Extensibility**: Adding emotion/mood/action is trivial - just add a property
4. **Error Recovery**: If the tool call fails, you get a structured error, not garbled text
5. **Testing**: Easy to mock and unit test with strongly-typed objects

### 18. Architecture Decision: Individual Bots vs. Webhooks (CRITICAL)

#### Current Implementation Issues
The existing TypeScript implementation uses **individual Discord bot accounts for each character**. This approach has several significant problems:

**Problems with Individual Bot Accounts:**
1. **Discord TOS Violation Risk**: Using multiple bot accounts that are controlled by a single system could violate Discord's Terms of Service, especially if they're automated to respond based on a central system
2. **Token Management**: Requires managing multiple bot tokens (one per character)
3. **Security**: Multiple tokens = larger attack surface
4. **Rate Limits**: Each bot has its own rate limits, but coordination is complex
5. **Maintenance Overhead**: Managing N bot applications in Discord Developer Portal
6. **Limited Flexibility**: Can't easily change character avatars/names dynamically
7. **Bot Presence**: All character bots must be online simultaneously, cluttering member lists

#### Recommended Solution: Discord Webhooks

**Use a single bot with webhooks for character messages.**

**Advantages of Webhooks:**
1. ✅ **TOS Compliant**: Fully supported Discord API feature
2. ✅ **Dynamic Identity**: Set name and avatar per message
3. ✅ **Single Token**: Only the manager bot token needed
4. ✅ **Easier Management**: One bot application in Discord Developer Portal
5. ✅ **Better Security**: Fewer tokens to secure
6. ✅ **Cleaner Server**: No clutter from multiple bot accounts in member list
7. ✅ **Flexible**: Can add/remove characters without creating new bots
8. ✅ **Programmatic Control**: Create/delete webhooks via API

**How Webhooks Work:**
```csharp
// Create a webhook for a channel (once, cache the URL)
var webhook = await channel.CreateWebhookAsync("Roleplay Characters");

// Send a message as any character
await webhook.ExecuteAsync(new DiscordWebhookBuilder()
    .WithContent("Hello everyone! Welcome to the Literature Club!")
    .WithUsername("Monika")  // Character name
    .WithAvatarUrl("https://example.com/monika.png")); // Character avatar
```

**Implementation Architecture:**
```
Main Bot (Manager)
├── Listens to messages in roleplay channels
├── Manages AI prompts and responses
├── Determines which character should respond
└── Executes webhook with character name/avatar

Webhooks (per channel)
├── Created/cached by bot on startup
├── Used to send messages with custom name/avatar
└── No separate bot accounts needed
```

#### Webhook Implementation in .NET

**Service Structure:**
```csharp
public class WebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly ConcurrentDictionary<ulong, DiscordWebhook> _webhookCache = new();

    public async Task<DiscordWebhook> GetOrCreateWebhookAsync(
        DiscordChannel channel, 
        CancellationToken cancellationToken = default)
    {
        if (_webhookCache.TryGetValue(channel.Id, out var cached))
            return cached;

        // Check if webhook already exists
        var webhooks = await channel.GetWebhooksAsync();
        var webhook = webhooks.FirstOrDefault(w => w.Name == "Roleplay Characters");
        
        if (webhook == null)
        {
            webhook = await channel.CreateWebhookAsync("Roleplay Characters", 
                reason: "For roleplay character messages");
            _logger.LogInformation("Created webhook for channel {ChannelName}", channel.Name);
        }

        _webhookCache[channel.Id] = webhook;
        return webhook;
    }

    public async Task SendAsCharacterAsync(
        DiscordChannel channel,
        Character character,
        string message,
        CancellationToken cancellationToken = default)
    {
        var webhook = await GetOrCreateWebhookAsync(channel, cancellationToken);
        
        var builder = new DiscordWebhookBuilder()
            .WithContent(message)
            .WithUsername(character.Name);
        
        if (!string.IsNullOrEmpty(character.AvatarUrl))
        {
            builder.WithAvatarUrl(character.AvatarUrl);
        }

        await webhook.ExecuteAsync(builder);
        _logger.LogInformation("Sent message as {Character} in {Channel}", 
            character.Name, channel.Name);
    }
}
```

**Character Model Update:**
```csharp
public record Character
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public string Personality { get; init; } = string.Empty;
    
    // No bot token needed!
    // Just avatar URL for webhook
    public string? AvatarUrl { get; init; }
}
```

**Configuration Update:**
```json
{
  "characters": [
    {
      "name": "Monika",
      "description": "President of the Literature Club",
      "personality": "Confident, self-aware, caring but with a darker side",
      "avatarUrl": "https://example.com/avatars/monika.png"
    },
    {
      "name": "Sayori",
      "description": "Vice President and childhood friend",
      "personality": "Cheerful, clumsy, struggles with depression",
      "avatarUrl": "https://example.com/avatars/sayori.png"
    }
  ]
}
```

#### Message Flow with Webhooks
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service with tool calling
4. AI returns: `{"character_name": "Monika", "message": "Hello!", "emotion": "happy"}`
5. Look up character by name
6. Send via webhook with character's name and avatar
7. Done! (No need to coordinate separate bot clients)

#### Webhook Limitations & Workarounds

**What Webhooks CAN'T Do:**
- Can't add reactions as the character (bot can react instead)
- Can't edit previous webhook messages easily (need to store message IDs)
- Can't show "typing" indicator as the character
- Don't appear in member list
- Can't have custom status/presence

**Workarounds:**
- **Reactions**: Have the manager bot add reactions if needed
- **Typing Indicator**: Skip it, or have manager bot type (minor immersion loss)
- **Message Editing**: Store webhook message IDs if editing is needed
- **Presence**: Not needed for this use case (characters only exist when speaking)

#### Migration Recommendation

**For the .NET migration, use webhooks exclusively:**

1. **Remove** all individual character bot token requirements
2. **Implement** webhook-based character messaging
3. **Simplify** configuration (one bot token + character avatars)
4. **Add** webhook caching for performance
5. **Keep** the AI character selection logic (tool calling)

**Benefits for .NET Implementation:**
- Simpler architecture (one `DiscordClient` instead of N+1)
- Fewer resources (one gateway connection vs. N+1)
- Better performance (no coordination between bots)
- Easier testing (mock one webhook service vs. N bots)
- More maintainable (less moving parts)

#### Code Comparison

**Before (Multiple Bots):**
```csharp
public class CharacterService
{
    private Dictionary<string, DiscordClient> _characterBots;
    
    public async Task SendMessageAsync(string characterName, string message)
    {
        var bot = _characterBots[characterName];
        await bot.SendMessageAsync(channelId, message);
    }
}
```

**After (Webhooks):**
```csharp
public class WebhookService
{
    private ConcurrentDictionary<ulong, DiscordWebhook> _webhooks;
    
    public async Task SendMessageAsync(DiscordChannel channel, Character character, string message)
    {
        var webhook = await GetOrCreateWebhookAsync(channel);
        await webhook.ExecuteAsync(new DiscordWebhookBuilder()
            .WithContent(message)
            .WithUsername(character.Name)
            .WithAvatarUrl(character.AvatarUrl));
    }
}
```

**Result**: Simpler, more reliable, fully TOS-compliant.

#### Required DSharpPlus Features
- `DiscordChannel.CreateWebhookAsync()` - Create webhooks
- `DiscordChannel.GetWebhooksAsync()` - List existing webhooks
- `DiscordWebhook.ExecuteAsync()` - Send messages via webhook
- `DiscordWebhookBuilder` - Build webhook messages with custom name/avatar

All of these are well-supported in DSharpPlus v5.x and Discord.Net v3.x.

#### Conclusion

**STRONGLY RECOMMENDED**: Use webhooks instead of individual bot accounts.

This is the modern, correct way to implement character-based messaging in Discord. The .NET migration provides a perfect opportunity to fix this architectural issue and create a more robust, maintainable, and TOS-compliant application.

### 19. Migration Checklist

- [ ] Create .NET 10 console application project
- [ ] Install required NuGet packages (DSharpPlus, etc.)
- [ ] Implement configuration models and loading
- [ ] Implement AI provider interfaces (OpenAI, Ollama)
- [ ] **Implement tool/function calling for character selection (recommended)**
- [ ] Implement legacy [CHAR]/[CONTENT] parser as fallback (optional)
- [ ] **Implement webhook service for character messaging (CRITICAL - replaces multiple bots)**
- [ ] Implement webhook caching per channel
- [ ] Implement channel service with conversation management
- [ ] Implement manager bot service with message handling
- [ ] Add idle conversation timer functionality
- [ ] Add logging throughout
- [ ] Create Dockerfile and docker-compose.yml
- [ ] Test webhook creation and message sending
- [ ] Test with multiple characters via webhooks
- [ ] Test character avatar/name display
- [ ] Test idle conversation feature
- [ ] Test with OpenAI API (tool calling)
- [ ] Test with Ollama (tool calling if supported, fallback otherwise)
- [ ] Document setup and configuration
- [ ] Create CI/CD pipeline (.github/workflows)

### 20. Advantages of .NET 10 Implementation

1. **Performance**: .NET 10 offers superior performance compared to Node.js
2. **Type Safety**: Strong typing reduces runtime errors
3. **Async/Await**: First-class async support throughout the framework
4. **Dependency Injection**: Built-in DI container
5. **Hosting**: Robust hosting model with graceful shutdown
6. **Logging**: Comprehensive logging framework
7. **Deployment**: Smaller Docker images with AOT compilation options
8. **Maintenance**: Strong tooling (Visual Studio, Rider, VS Code)

### 21. Potential Challenges

1. **Discord Library Differences**: discord.js and DSharpPlus have different APIs
2. **~~Multiple Client Management~~ SOLVED**: Using webhooks eliminates the need for multiple Discord clients
3. **JSON Configuration**: Ensuring compatibility with existing config format
4. **Event Handling**: Different event models between Node.js and .NET
5. **HTTP Client Configuration**: Setting up IHttpClientFactory correctly
6. **Tool Calling API Differences**: OpenAI and Ollama may have slightly different tool calling implementations
7. **Webhook Management**: Caching webhooks and handling webhook recreation if deleted

### 22. Estimated Development Time

**With Webhook Architecture (Recommended):**
- **Basic Implementation**: 12-18 hours (simpler than multi-bot approach)
- **Webhook Service**: 2-3 hours
- **Testing & Debugging**: 6-10 hours
- **Documentation**: 4-6 hours
- **Docker Setup**: 2-4 hours
- **Total**: ~26-41 hours for a complete migration

**Time Savings from Webhooks**: ~4-5 hours saved by not managing multiple Discord clients

## Conclusion

Migrating this Discord roleplay bot from TypeScript/Node.js to .NET 10 is highly feasible and would benefit from .NET's performance, type safety, and robust ecosystem. The main requirements are:

1. **DSharpPlus or Discord.Net** for Discord integration
2. **Discord Webhooks** for character messaging (single bot, multiple character identities)
3. **Tool/Function Calling** for reliable character selection (instead of regex parsing)
4. **IHttpClientFactory** for AI API calls (OpenAI/Ollama)
5. **System.Text.Json** for configuration
6. **Microsoft.Extensions.Hosting** for application lifecycle

### Key Architectural Improvements in .NET Version:

1. **Webhooks Instead of Multiple Bots**: Simpler, TOS-compliant, easier to maintain
2. **Tool Calling Instead of Regex**: More reliable character selection with structured output
3. **Strong Typing**: C# models for all configuration and responses
4. **Built-in DI/Logging**: Leveraging .NET's robust infrastructure

The .NET implementation would be **simpler and more maintainable** than the TypeScript version due to webhooks replacing multi-bot coordination and tool calling replacing regex parsing.

## References

- [DSharpPlus Documentation](https://dsharpplus.github.io/)
- [Discord.Net Documentation](https://docs.discordnet.dev/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Ollama API Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
