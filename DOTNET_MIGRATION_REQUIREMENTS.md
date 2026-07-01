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
├── Appear as members in the server
└── Can be interacted with directly (DMs, reactions, etc.)
```

**✅ NOTE**: This architecture uses individual bot accounts per character for **immersion and interaction**. Characters appear as actual server members and can be messaged directly, creating a more realistic roleplay experience. See Section 18 for architectural details and alternative approaches.

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
│   ├── CharacterBotService.cs          # Manages individual character bot clients
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
    
    // For individual bot approach
    public string BotToken { get; init; } = string.Empty;
    
    // Optional: Avatar URL (for webhooks alternative or bot profile)
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
- Each character bot appears as a member in the Discord server
- Characters can receive direct messages and interact with server features

#### Message Flow (Individual Bots with Tool Calling)
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service with tool calling
4. AI returns structured response: `{"character_name": "Monika", "message": "Hello!"}`
5. Look up matching character bot by name
6. Character bot sends the message (appears as that character in server)

#### Benefits of Individual Bot Approach
- **Immersion**: Characters appear as actual members in the server
- **Direct Interaction**: Users can DM characters, see them in member lists
- **Server Integration**: Characters can have roles, permissions, custom status
- **Reactions & Embeds**: Characters can react to messages, post embeds as themselves
- **Typing Indicators**: Show typing as the character
- **Presence**: Characters can have custom status messages and activities

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
        services.AddSingleton<CharacterBotService>();  // Manages individual character bots
        services.AddSingleton<ChannelService>();
        
        // Background services
        services.AddHostedService<ManagerBotService>();
        services.AddHostedService<CharacterBotService>();  // Start character bots
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
- Securely store N+1 bot tokens (manager + each character)

#### API Key Storage
```csharp
// appsettings.json (not in git)
{
  "CoreConfig": {
    "ManagerBot": {
      "BotToken": "env:MANAGER_BOT_TOKEN"
    },
    "AiToken": "env:OPENAI_API_KEY"
  },
  "Characters": [
    {
      "Name": "Monika",
      "BotToken": "env:MONIKA_BOT_TOKEN",
      "Description": "...",
      "Personality": "..."
    },
    {
      "Name": "Sayori",
      "BotToken": "env:SAYORI_BOT_TOKEN",
      "Description": "...",
      "Personality": "..."
    }
  ]
}
```

#### Setting Up Multiple Bot Applications

For the individual bot approach, you'll need to create a bot application in Discord Developer Portal for each character:

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application for the manager bot
3. Create a new application for each character (Monika, Sayori, etc.)
4. For each application:
   - Enable "Message Content Intent" under Bot → Privileged Gateway Intents
   - Copy the bot token
   - Invite the bot to your server with appropriate permissions
5. Store all tokens securely in your configuration

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

### 18. Architecture Decision: Individual Bots vs. Webhooks

#### Chosen Approach: Individual Bot Accounts

The current implementation uses **individual Discord bot accounts for each character**, and this approach will be preserved in the .NET migration for the following reasons:

**Advantages of Individual Bot Accounts:**
1. ✅ **Immersion**: Characters appear as actual members in the Discord server
2. ✅ **Direct Interaction**: Users can send DMs to characters, mention them, etc.
3. ✅ **Server Integration**: Characters can have custom roles, permissions, and status
4. ✅ **Full API Access**: Characters can use reactions, embeds, typing indicators
5. ✅ **Presence & Activities**: Characters can show custom status messages
6. ✅ **Member List Visibility**: Characters appear in member lists (part of the roleplay immersion)
7. ✅ **Natural Discord Experience**: Feels like interacting with actual server members

**Discord TOS Compliance:**
Using multiple bot accounts is **fully compliant with Discord's Terms of Service**, as long as:
- Each bot is a legitimate bot application (not a selfbot/user account)
- Bots are not used for spam, abuse, or malicious purposes
- Each bot has its own valid bot token from the Discord Developer Portal
- Bots follow Discord's API rate limits and guidelines

Multiple bot accounts are a common pattern for applications that need distinct identities, and Discord explicitly supports this use case.

**Implementation Considerations:**
1. **Token Management**: Store each character's bot token securely in configuration
2. **Client Management**: Manage N+1 Discord clients (1 manager + N characters)
3. **Lifecycle**: Start/stop all character bots alongside the manager bot
4. **Rate Limits**: Each bot has independent rate limits (beneficial for high-activity scenarios)
5. **Event Handling**: Only the manager bot needs to listen to messages; character bots primarily send messages

#### Individual Bot Implementation in .NET

**Service Structure:**
```csharp
public class CharacterBotService : BackgroundService
{
    private readonly ILogger<CharacterBotService> _logger;
    private readonly Dictionary<string, DiscordClient> _characterClients = new();
    private readonly List<Character> _characters;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var character in _characters)
        {
            var config = new DiscordConfiguration
            {
                Token = character.BotToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages
            };

            var client = new DiscordClient(config);
            
            // Optional: Set custom status for character
            client.Ready += async (s, e) =>
            {
                await client.UpdateStatusAsync(
                    new DiscordActivity($"Roleplay as {character.Name}", ActivityType.Playing));
                _logger.LogInformation("Character bot {Name} is ready", character.Name);
            };

            await client.ConnectAsync();
            _characterClients[character.Name] = client;
        }

        // Keep running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async Task SendAsCharacterAsync(
        string characterName,
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!_characterClients.TryGetValue(characterName, out var client))
        {
            _logger.LogWarning("Character bot {Name} not found", characterName);
            return;
        }

        var channel = await client.GetChannelAsync(channelId);
        await channel.SendMessageAsync(message);
        
        _logger.LogInformation("Sent message as {Character} in channel {ChannelId}", 
            characterName, channelId);
    }

    public async Task ShowTypingAsync(string characterName, ulong channelId)
    {
        if (_characterClients.TryGetValue(characterName, out var client))
        {
            var channel = await client.GetChannelAsync(channelId);
            await channel.TriggerTypingAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var client in _characterClients.Values)
        {
            await client.DisconnectAsync();
            client.Dispose();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
```

**Configuration Format:**
```json
{
  "characters": [
    {
      "name": "Monika",
      "description": "President of the Literature Club",
      "personality": "Confident, self-aware, caring but with a darker side",
      "botToken": "MTIzNDU2Nzg5MDEyMzQ1Njc4.AbCdEf.GhIjKlMnOpQrStUvWxYz",
      "avatarUrl": "https://example.com/avatars/monika.png"
    },
    {
      "name": "Sayori",
      "description": "Vice President and childhood friend",
      "personality": "Cheerful, clumsy, struggles with depression",
      "botToken": "OTg3NjU0MzIxMDk4NzY1NDMyLkFiQ2RlZi5HaElqS2xNbk9wUXJTdFU=",
      "avatarUrl": "https://example.com/avatars/sayori.png"
    }
  ]
}
```

#### Message Flow with Individual Bots
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service with tool calling
4. AI returns: `{"character_name": "Monika", "message": "Hello!", "emotion": "happy"}`
5. Look up character by name
6. Optionally: Trigger typing indicator for that character bot
7. Character bot sends the message in the channel
8. Message appears from the character bot account

#### Direct Message Handling
Since characters are real bot accounts, they can receive and respond to direct messages:

```csharp
public class CharacterBotService
{
    public void RegisterMessageHandlers()
    {
        foreach (var (name, client) in _characterClients)
        {
            client.MessageCreated += async (s, e) =>
            {
                // Handle DMs to this character
                if (e.Channel.IsPrivate && !e.Author.IsBot)
                {
                    await HandleDirectMessageAsync(name, e.Message);
                }
            };
        }
    }

    private async Task HandleDirectMessageAsync(string characterName, DiscordMessage message)
    {
        // Build conversation context for this character
        // Send to AI with character's personality
        // Respond as the character
    }
}
```

#### Alternative: Discord Webhooks

For simpler use cases where immersion and direct interaction aren't critical, **webhooks** offer an alternative approach:

**Webhook Advantages:**
- Simpler architecture (one Discord client instead of N+1)
- Dynamic name/avatar per message
- Easier configuration (no need for N bot tokens)
- Lower resource usage (one gateway connection)

**Webhook Limitations:**
- ❌ Characters don't appear in member lists
- ❌ Can't receive direct messages
- ❌ Can't show typing indicators as character
- ❌ Can't have custom status/presence
- ❌ Limited interaction capabilities

**When to Use Webhooks:**
- Simple chat-only bots where characters just send messages
- No need for DM functionality
- Immersion isn't a priority
- Want to minimize configuration complexity

**When to Use Individual Bots (Recommended):**
- ✅ Immersion is important (characters feel like real server members)
- ✅ Want DM functionality (users can message characters)
- ✅ Want full Discord feature support (reactions, typing, status, etc.)
- ✅ Characters should appear in member lists and have roles/permissions
- ✅ Want the most natural roleplay experience

#### Implementation Decision

**For this project: Individual bot accounts are preferred** to maintain immersion and enable direct interaction between users and characters.

#### Required DSharpPlus Features for Individual Bots
- `DiscordClient` - Multiple instances (one per character + manager)
- `DiscordConfiguration` - Configure each client independently
- `DiscordIntents.Guilds` and `DiscordIntents.GuildMessages` - Required intents
- `DiscordClient.ConnectAsync()` / `DisconnectAsync()` - Lifecycle management
- `DiscordChannel.SendMessageAsync()` - Send messages as characters
- `DiscordChannel.TriggerTypingAsync()` - Show typing indicators
- `DiscordClient.UpdateStatusAsync()` - Set character status/activities

All of these are well-supported in DSharpPlus v5.x and Discord.Net v3.x.

### 19. Migration Checklist

- [ ] Create .NET 10 console application project
- [ ] Install required NuGet packages (DSharpPlus, etc.)
- [ ] Implement configuration models and loading
- [ ] Implement AI provider interfaces (OpenAI, Ollama)
- [ ] **Implement tool/function calling for character selection (recommended)**
- [ ] Implement legacy [CHAR]/[CONTENT] parser as fallback (optional)
- [ ] **Implement CharacterBotService for managing multiple Discord clients**
- [ ] Implement character bot lifecycle (connect/disconnect all character bots)
- [ ] Implement message sending through appropriate character bot
- [ ] Implement channel service with conversation management
- [ ] Implement manager bot service with message handling
- [ ] Add idle conversation timer functionality
- [ ] Add logging throughout
- [ ] Create Dockerfile and docker-compose.yml
- [ ] Test character bot connections (all bots come online)
- [ ] Test with multiple characters sending messages
- [ ] Test character presence in member list
- [ ] Test typing indicators as characters
- [ ] Test direct messages to character bots (optional feature)
- [ ] Test idle conversation feature
- [ ] Test with OpenAI API (tool calling)
- [ ] Test with Ollama (tool calling if supported, fallback otherwise)
- [ ] Document setup and configuration (including bot token setup for each character)
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
2. **Multiple Client Management**: Managing N+1 Discord clients (lifecycle, events, rate limits)
3. **JSON Configuration**: Ensuring compatibility with existing config format, handling N bot tokens
4. **Event Handling**: Different event models between Node.js and .NET
5. **HTTP Client Configuration**: Setting up IHttpClientFactory correctly
6. **Tool Calling API Differences**: OpenAI and Ollama may have slightly different tool calling implementations
7. **Client Synchronization**: Ensuring all character bots are ready before accepting messages
8. **Resource Management**: Properly disposing N+1 Discord clients on shutdown
9. **Token Security**: Securely managing multiple bot tokens in configuration
10. **Bot Setup Overhead**: Creating and configuring N bot applications in Discord Developer Portal

### 22. Estimated Development Time

**With Individual Bot Architecture:**
- **Basic Implementation**: 16-22 hours
- **Character Bot Service**: 4-6 hours (managing N+1 Discord clients)
- **DM Handling (Optional)**: 3-4 hours
- **Testing & Debugging**: 8-12 hours
- **Documentation**: 4-6 hours
- **Docker Setup**: 2-4 hours
- **Total**: ~37-54 hours for a complete migration with full features

**Note**: The individual bot approach requires more development time but provides significantly better immersion and interaction capabilities.

## Conclusion

Migrating this Discord roleplay bot from TypeScript/Node.js to .NET 10 is highly feasible and would benefit from .NET's performance, type safety, and robust ecosystem. The main requirements are:

1. **DSharpPlus or Discord.Net** for Discord integration
2. **Multiple Discord Clients** for character bots (N+1 clients: 1 manager + N characters)
3. **Tool/Function Calling** for reliable character selection (instead of regex parsing)
4. **IHttpClientFactory** for AI API calls (OpenAI/Ollama)
5. **System.Text.Json** for configuration
6. **Microsoft.Extensions.Hosting** for application lifecycle and background services

### Key Architectural Decisions in .NET Version:

1. **Individual Bot Accounts**: Maintains immersion and enables direct interaction with characters
2. **Tool Calling Instead of Regex**: More reliable character selection with structured output
3. **Strong Typing**: C# models for all configuration and responses
4. **Built-in DI/Logging**: Leveraging .NET's robust infrastructure
5. **Background Services**: CharacterBotService manages all character bot lifecycles

The .NET implementation would be **more robust and feature-rich** than the TypeScript version, with better type safety, performance, and the ability to handle direct messages and advanced Discord features through individual bot accounts.

## References

- [DSharpPlus Documentation](https://dsharpplus.github.io/)
- [Discord.Net Documentation](https://docs.discordnet.dev/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Ollama API Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
