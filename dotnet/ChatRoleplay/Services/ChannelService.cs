using ChatRoleplay.Config;
using ChatRoleplay.Models;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

/// <summary>
/// Manages the conversation state and message handling for a single Discord roleplay channel.
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

    // Fallback Discord client (manager bot) used when no character bot is available
    private readonly DSharpPlus.DiscordClient _managerClient;

    private Timer? _idleTimer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ChannelService(
        ulong channelId,
        List<ChatMessage> prompt,
        AiService aiService,
        CharacterBotService characterBotService,
        List<Character> characters,
        CoreConfig coreConfig,
        DSharpPlus.DiscordClient managerClient,
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
    /// Starts the idle timer so the characters will send messages autonomously.
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
    /// </summary>
    public async Task HandleMessageAsync(
        DSharpPlus.EventArgs.MessageCreateEventArgs e,
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

            // Show typing in the chosen character before we have an answer
            // (we don't know which character yet, so pick the first available)
            var firstChar = _characters.FirstOrDefault(c => _characterBotService.HasCharacter(c.Name));
            if (firstChar != null)
                await _characterBotService.TriggerTypingAsync(firstChar.Name, _channelId);

            var response = await _aiService.GetCompletionAsync(_prompt, _characters, cancellationToken);

            _logger.LogInformation("[RESPONSE] {CharName}: {Message}",
                response?.CharacterName, response?.Message);

            if (response == null || string.IsNullOrWhiteSpace(response.Message))
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

            if (response == null || string.IsNullOrWhiteSpace(response.Message))
            {
                _logger.LogWarning("No valid idle response for channel {ChannelId}", _channelId);
                ResetIdleTimer();
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
        if (_idleTimer != null)
            await _idleTimer.DisposeAsync();
        _semaphore.Dispose();
    }
}
