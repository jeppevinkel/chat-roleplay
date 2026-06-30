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
6. **Message Parsing**: Custom format for character responses: `[CHAR] Name [CONTENT] Message`

### Architecture
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
│   ├── Character.cs                    # Character model
│   ├── CoreConfig.cs                   # Core configuration model
│   ├── PromptConfig.cs                 # Prompt configuration model
│   ├── CharacterConfig.cs              # Character configuration model
│   └── Message.cs                      # Message model
├── Services/
│   ├── IConfigService.cs               # Configuration service interface
│   ├── ConfigService.cs                # Configuration service implementation
│   ├── IAiService.cs                   # AI service interface
│   ├── AiService.cs                    # AI service implementation (OpenAI/Ollama)
│   ├── CharacterService.cs             # Character bot management
│   ├── ChannelService.cs               # Channel/conversation management
│   └── ManagerBotService.cs            # Main bot service
├── Providers/
│   ├── ILlmProvider.cs                 # LLM provider interface
│   ├── OpenAiProvider.cs               # OpenAI implementation
│   └── OllamaProvider.cs               # Ollama implementation
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
    public string BotToken { get; init; } = string.Empty;
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

**CharacterService.cs**
```csharp
public class CharacterService
{
    // Manages multiple DiscordClient instances (one per character)
    // Handles login and message sending for each character
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

#### Message Flow
1. Manager bot receives message in roleplay channel
2. Add message to conversation history
3. Send prompt to AI service
4. Parse response for `[CHAR]` and `[CONTENT]`
5. Find matching character bot
6. Character bot replies to message

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
        services.AddSingleton<CharacterService>();
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

### 17. Migration Checklist

- [ ] Create .NET 10 console application project
- [ ] Install required NuGet packages (DSharpPlus, etc.)
- [ ] Implement configuration models and loading
- [ ] Implement AI provider interfaces (OpenAI, Ollama)
- [ ] Implement character service with multiple Discord clients
- [ ] Implement channel service with conversation management
- [ ] Implement manager bot service with message handling
- [ ] Add idle conversation timer functionality
- [ ] Implement message parsing for [CHAR]/[CONTENT] format
- [ ] Add logging throughout
- [ ] Create Dockerfile and docker-compose.yml
- [ ] Test with single character
- [ ] Test with multiple characters
- [ ] Test idle conversation feature
- [ ] Test with OpenAI API
- [ ] Test with Ollama
- [ ] Document setup and configuration
- [ ] Create CI/CD pipeline (.github/workflows)

### 18. Advantages of .NET 10 Implementation

1. **Performance**: .NET 10 offers superior performance compared to Node.js
2. **Type Safety**: Strong typing reduces runtime errors
3. **Async/Await**: First-class async support throughout the framework
4. **Dependency Injection**: Built-in DI container
5. **Hosting**: Robust hosting model with graceful shutdown
6. **Logging**: Comprehensive logging framework
7. **Deployment**: Smaller Docker images with AOT compilation options
8. **Maintenance**: Strong tooling (Visual Studio, Rider, VS Code)

### 19. Potential Challenges

1. **Discord Library Differences**: discord.js and DSharpPlus have different APIs
2. **Multiple Client Management**: Managing multiple Discord clients in .NET requires careful resource management
3. **JSON Configuration**: Ensuring compatibility with existing config format
4. **Event Handling**: Different event models between Node.js and .NET
5. **HTTP Client Configuration**: Setting up IHttpClientFactory correctly

### 20. Estimated Development Time

- **Basic Implementation**: 16-24 hours
- **Testing & Debugging**: 8-12 hours
- **Documentation**: 4-6 hours
- **Docker Setup**: 2-4 hours
- **Total**: ~30-46 hours for a complete migration

## Conclusion

Migrating this Discord roleplay bot from TypeScript/Node.js to .NET 10 is highly feasible and would benefit from .NET's performance, type safety, and robust ecosystem. The main requirements are:

1. **DSharpPlus or Discord.Net** for Discord integration
2. **IHttpClientFactory** for AI API calls
3. **System.Text.Json** for configuration
4. **Microsoft.Extensions.Hosting** for application lifecycle
5. Proper multi-client management for character bots

The architecture would remain largely the same, with services handling different aspects (AI, Characters, Channels, Manager Bot), but leveraging .NET's built-in dependency injection, configuration, and logging frameworks for a more maintainable codebase.

## References

- [DSharpPlus Documentation](https://dsharpplus.github.io/)
- [Discord.Net Documentation](https://docs.discordnet.dev/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Ollama API Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
