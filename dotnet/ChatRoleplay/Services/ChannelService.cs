using ChatRoleplay.Config;
using ChatRoleplay.Models;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

/// <summary>
/// Manages the conversation state and message handling for a single Discord roleplay channel.
/// Uses the DSharpPlus 5.x API.
/// </summary>
public class ChannelService : IAsyncDisposable
{
    private readonly ulong _channelId;
    private readonly List<ChatMessage> _prompt;
    private readonly AiService _aiService;
    private readonly CharacterBotService _characterBotService;
    private readonly List<Character> _characters;
    private readonly CoreConfig _coreConfig;
    private readonly ILogger<ChannelService> _logger;

    // Fallback Discord client (manager bot) used when no character bot is available for a character.
    private readonly DiscordClient _managerClient;

    private Timer? _idleTimer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ChannelService(
        ulong channelId,
        List<ChatMessage> prompt,
        AiService aiService,
        CharacterBotService characterBotService,
        List<Character> characters,
        CoreConfig coreConfig,
        DiscordClient managerClient,
        ILogger<ChannelService> logger)
    {
        _channelId = channelId;
        _prompt = prompt;
        _aiService = aiService;
        _characterBotService = characterBotService;
        _characters = characters;
        _coreConfig = coreConfig;
        _managerClient = managerClient;
        _logger = logger;
    }

    /// <summary>
    /// Starts the idle timer so characters will send messages autonomously when the channel is quiet.
    /// </summary>
    public void StartIdleTimer()
    {
        _idleTimer = new Timer(
            callback: _ => _ = ContinueIdleAsync(),
            state: null,
            dueTime: TimeSpan.FromSeconds(_coreConfig.IdleIntervalSec),
            period: Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Handles an incoming user message in the channel.
    /// Called from the manager bot's <c>MessageCreated</c> event.
    /// </summary>
    public async Task HandleMessageAsync(
        MessageCreatedEventArgs e,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            ResetIdleTimer();

            _prompt.Add(new ChatMessage("user",
                $"[NAME] {e.Author.Username} [MSG] {e.Message.Content}"));

            _logger.LogInformation("[MESSAGE] [NAME] {User} [MSG] {Content}",
                e.Author.Username, e.Message.Content);

            // Trigger typing on whichever character bot is available while the AI thinks.
            var firstChar = _characters.FirstOrDefault(c => _characterBotService.HasCharacter(c.Name));
            if (firstChar is not null)
                await _characterBotService.TriggerTypingAsync(firstChar.Name, _channelId);

            var response = await _aiService.GetCompletionAsync(_prompt, _characters, cancellationToken);

            _logger.LogInformation("[RESPONSE] {CharName}: {Message}",
                response?.CharacterName, response?.Message);

            if (response is null || string.IsNullOrWhiteSpace(response.Message))
            {
                _logger.LogWarning("No valid response extracted for channel {ChannelId}", _channelId);
                return;
            }

            _prompt.Add(new ChatMessage("assistant",
                $"[CHAR] {response.CharacterName} [CONTENT] {response.Message}"));

            if (_characterBotService.HasCharacter(response.CharacterName))
            {
                await _characterBotService.ReplyAsCharacterAsync(
                    response.CharacterName, _channelId, e.Message.Id, response.Message, cancellationToken);
            }
            else
            {
                // Fall back to the manager bot replying
                await e.Message.RespondAsync(response.Message);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ContinueIdleAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _prompt.Add(new ChatMessage("user", "continue"));

            var response = await _aiService.GetCompletionAsync(_prompt, _characters, CancellationToken.None);

            _logger.LogInformation("[IDLE RESPONSE] {CharName}: {Message}",
                response?.CharacterName, response?.Message);

            if (response is null || string.IsNullOrWhiteSpace(response.Message))
            {
                _logger.LogWarning("No valid idle response for channel {ChannelId}", _channelId);
                return;
            }

            _prompt.Add(new ChatMessage("assistant",
                $"[CHAR] {response.CharacterName} [CONTENT] {response.Message}"));

            if (_characterBotService.HasCharacter(response.CharacterName))
            {
                await _characterBotService.TriggerTypingAsync(response.CharacterName, _channelId);
                await Task.Delay(500); // brief pause for realism
                await _characterBotService.SendAsCharacterAsync(
                    response.CharacterName, _channelId, response.Message);
            }
            else
            {
                var channel = await _managerClient.GetChannelAsync(_channelId);
                await channel.SendMessageAsync(response.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in idle continuation for channel {ChannelId}", _channelId);
        }
        finally
        {
            ResetIdleTimer();
            _semaphore.Release();
        }
    }

    private void ResetIdleTimer()
    {
        _idleTimer?.Change(
            TimeSpan.FromSeconds(_coreConfig.IdleIntervalSec),
            Timeout.InfiniteTimeSpan);
    }

    public async ValueTask DisposeAsync()
    {
        if (_idleTimer is not null)
            await _idleTimer.DisposeAsync();
        _semaphore.Dispose();
    }
}
