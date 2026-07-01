# Chat-Roleplay (.NET Edition)

A complete .NET 10 reimplementation of the Chat-Roleplay Discord bot, written in C# using [DSharpPlus 5.x](https://dsharpplus.github.io/).  
It is **feature-compatible** with the original TypeScript/Node.js application and lives in the `dotnet/` subfolder so it doesn't interfere with the existing project.

## Features

- **Multiple character bots** – each character has its own Discord bot account and appears as a real server member.
- **AI-powered responses** – supports OpenAI-compatible APIs and Ollama (local LLM).
- **Tool/function calling** – uses structured output from the AI instead of fragile regex parsing.
- **Legacy format fallback** – still understands `[CHAR] Name [CONTENT] Message` if the model doesn't support tool calling.
- **Idle conversation** – characters will continue talking on their own after a configurable timeout.
- **Typing indicators** – characters show a typing indicator before replying.
- **Docker support** – comes with a `Dockerfile` and `docker-compose.yml`.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- One Discord bot application per character **plus** one manager bot  
  (see [Setup in Discord Developer Portal](#setup-in-discord-developer-portal))

### Run Locally

```bash
cd dotnet/ChatRoleplay
dotnet run
```

On the first run the application will create a `data/` folder next to the binary with default config files.  
Stop the application, edit the configs, and restart.

### Run with Docker

```bash
cd dotnet
docker compose up --build
```

Config files will appear in `dotnet/chat-roleplay-data/` on the host.

---

## Configuration

All configuration lives in the `data/` directory (created automatically on first run).

### `data/core-config.json`

```json
{
  "managerBot": {
    "botToken": "<MANAGER BOT TOKEN>"
  },
  "model": "gpt-4o",
  "provider": "openai",
  "customEndpoint": null,
  "aiToken": "<OPENAI API KEY>",
  "idleIntervalSec": 180,
  "roleplayChannels": [
    "<CHANNEL ID>"
  ],
  "useToolCalling": true
}
```

| Field | Description |
|-------|-------------|
| `managerBot.botToken` | Token for the manager (listener) bot |
| `model` | AI model name (e.g. `gpt-4o`, `llama3.1`) |
| `provider` | `"openai"` or `"ollama"` |
| `customEndpoint` | Override the base URL (e.g. for a local OpenAI-compatible server) |
| `aiToken` | API key – only used for the `openai` provider |
| `idleIntervalSec` | Seconds of inactivity before characters spontaneously continue |
| `roleplayChannels` | List of Discord channel IDs where roleplay is active |
| `useToolCalling` | `true` to use tool/function calling; `false` for legacy text format |

### `data/character-config.json`

```json
{
  "characters": [
    {
      "name": "Okabe Rintaro",
      "description": "The founder of the Future Gadget Lab",
      "longDescription": "My name is Hououin Kyouma! ...",
      "personality": "Okabe is often acting delusional and grandiose, but cares a lot about his friends",
      "botToken": "<CHARACTER BOT TOKEN>"
    }
  ]
}
```

Add as many characters as you like. Each needs its own Discord bot token.

### `data/prompt-config.json`

The system prompt template. The following placeholders are replaced at runtime:

| Placeholder | Replaced with |
|-------------|--------------|
| `{num_chars}` | Number of characters |
| `{chars}` | Comma-separated list of character names |
| `{chars_personality}` | All personality descriptions joined by `. ` |

---

## Setup in Discord Developer Portal

1. Go to [https://discord.com/developers/applications](https://discord.com/developers/applications).
2. Create one application for the **manager bot**.
3. Create one application for **each character**.
4. For every application:
   - Navigate to **Bot** → enable **Message Content Intent** (privileged gateway intent).
   - Copy the bot token and paste it into the appropriate config field.
   - Invite each bot to your server with the **Send Messages** and **Read Message History** permissions.

---

## Project Structure

```
dotnet/
├── ChatRoleplay.sln
├── Dockerfile
├── docker-compose.yml
├── README.md
└── ChatRoleplay/
    ├── ChatRoleplay.csproj
    ├── Program.cs                    # Entry point & DI wiring
    ├── Config/
    │   ├── CoreConfig.cs             # Core configuration model
    │   ├── CharacterConfig.cs        # Character list model
    │   └── PromptConfig.cs           # Prompt template model
    ├── Models/
    │   ├── Character.cs              # Character data model
    │   ├── ChatMessage.cs            # AI message model
    │   └── CharacterResponse.cs      # Parsed AI response
    └── Services/
        ├── ConfigService.cs          # JSON config loading/saving
        ├── AiService.cs              # OpenAI / Ollama integration
        ├── CharacterBotService.cs    # Manages N character Discord clients
        ├── ChannelService.cs         # Per-channel conversation state
        └── ManagerBotService.cs      # Main hosted service / manager bot
```

---

## Differences from the Node.js Version

| Feature | Node.js | .NET |
|---------|---------|------|
| Discord library | discord.js | DSharpPlus |
| AI character selection | Regex on `[CHAR]…[CONTENT]` | Tool/function calling (with legacy fallback) |
| Config loading | Custom `BaseConfig` class | `ConfigService` + `System.Text.Json` |
| Async model | Promises / callbacks | `async`/`await` + `CancellationToken` |
| Hosting | Plain Node process | `Microsoft.Extensions.Hosting` |
| Logging | `console.log` | `Microsoft.Extensions.Logging` |
| Idle timer | `setTimeout` | `System.Threading.Timer` |
| Container | `node:22` | `mcr.microsoft.com/dotnet/runtime:10.0` |
