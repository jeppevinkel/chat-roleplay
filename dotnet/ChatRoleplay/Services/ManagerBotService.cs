using ChatRoleplay.Config;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

/// <summary>
/// The main hosted service that runs the manager Discord bot and orchestrates
/// character bots and channel services.
/// </summary>
public class ManagerBotService : BackgroundService
{
    private readonly ILogger<ManagerBotService> _logger;
    private readonly ILogger<ChannelService> _channelLogger;
    private readonly ConfigService _configService;
    private readonly CharacterBotService _characterBotService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    private DiscordClient? _managerClient;
    private readonly Dictionary<ulong, ChannelService> _channels = new();

    public ManagerBotService(
        ILogger<ManagerBotService> logger,
        ILogger<ChannelService> channelLogger,
        ConfigService configService,
        CharacterBotService characterBotService,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _channelLogger = channelLogger;
        _configService = configService;
        _characterBotService = characterBotService;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ── 1. Load configuration ─────────────────────────────────────────
        await _configService.LoadAllAsync();

        var coreConfig = _configService.CoreConfig;
        var characterConfig = _configService.CharacterConfig;
        var promptConfig = _configService.PromptConfig;

        if (string.IsNullOrWhiteSpace(coreConfig.ManagerBot.BotToken))
        {
            _logger.LogError("Manager bot token is not configured. Edit data/core-config.json and restart.");
            return;
        }

        // ── 2. Start character bots ───────────────────────────────────────
        await _characterBotService.StartAllAsync(characterConfig.Characters, stoppingToken);

        // ── 3. Create the AI service ──────────────────────────────────────
        var aiService = new AiService(
            _httpClientFactory,
            _loggerFactory.CreateLogger<AiService>(),
            coreConfig);

        // ── 4. Connect the manager bot ────────────────────────────────────
        var config = new DiscordConfiguration
        {
            Token = coreConfig.ManagerBot.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages | DiscordIntents.MessageContents,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
        };

        _managerClient = new DiscordClient(config);

        _managerClient.Ready += (_, _) =>
        {
            _logger.LogInformation("Manager bot connected");

            // Build channel services once the manager is ready
            var formattedPrompt = promptConfig.GetFormattedTemplate(characterConfig.Characters);

            foreach (var channelId in coreConfig.RoleplayChannels)
            {
                var channelService = new ChannelService(
                    channelId,
                    formattedPrompt.Select(m => new Models.ChatMessage(m.Role, m.Content)).ToList(),
                    aiService,
                    _characterBotService,
                    characterConfig.Characters,
                    coreConfig,
                    _managerClient,
                    _channelLogger);

                _channels[channelId] = channelService;
                channelService.StartIdleTimer();
                _logger.LogInformation("Watching roleplay channel: {ChannelId}", channelId);
            }

            if (coreConfig.RoleplayChannels.Count == 0)
                _logger.LogWarning("No roleplay channels configured. Add channel IDs to data/core-config.json.");

            return Task.CompletedTask;
        };

        _managerClient.MessageCreated += OnMessageCreatedAsync;

        await _managerClient.ConnectAsync();

        // Keep running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    private async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs e)
    {
        // Ignore all bots (manager + character bots)
        if (e.Author.IsBot) return;

        if (!_channels.TryGetValue(e.Channel.Id, out var channel)) return;

        try
        {
            await channel.HandleMessageAsync(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing message in channel {ChannelId}", e.Channel.Id);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down manager bot service…");

        foreach (var channel in _channels.Values)
            await channel.DisposeAsync();

        if (_managerClient != null)
        {
            await _managerClient.DisconnectAsync();
            _managerClient.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
