using ChatRoleplay.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChatRoleplay.Services;

/// <summary>
/// Manages one Discord client per character bot.
/// Each character has its own bot token and appears as a real server member.
/// </summary>
public class CharacterBotService : IAsyncDisposable
{
    private readonly ILogger<CharacterBotService> _logger;
    private readonly Dictionary<string, DiscordClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public CharacterBotService(ILogger<CharacterBotService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts (connects) a Discord client for every character that has a valid bot token.
    /// </summary>
    public async Task StartAllAsync(List<Character> characters, CancellationToken cancellationToken)
    {
        foreach (var character in characters)
        {
            if (string.IsNullOrWhiteSpace(character.BotToken) ||
                character.BotToken == "DISCORD_BOT_TOKEN")
            {
                _logger.LogWarning("Character {Name} has no valid bot token – skipping", character.Name);
                continue;
            }

            try
            {
                var config = new DiscordConfiguration
                {
                    Token = character.BotToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages,
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                };

                var client = new DiscordClient(config);

                var name = character.Name; // capture for closure
                client.Ready += (_, _) =>
                {
                    _logger.LogInformation("Character bot ready: {Name}", name);
                    return Task.CompletedTask;
                };

                await client.ConnectAsync();
                _clients[character.Name] = client;
                _logger.LogInformation("Connected character bot: {Name}", character.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect character bot for {Name}", character.Name);
            }
        }
    }

    /// <summary>
    /// Sends a message as the specified character in the given channel.
    /// </summary>
    public async Task SendAsCharacterAsync(
        string characterName,
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!_clients.TryGetValue(characterName, out var client))
        {
            _logger.LogWarning("No client found for character: {Name}", characterName);
            return;
        }

        try
        {
            var channel = await client.GetChannelAsync(channelId);
            await channel.SendMessageAsync(message);
            _logger.LogDebug("Sent message as {Character} in channel {ChannelId}", characterName, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message as character {Name}", characterName);
        }
    }

    /// <summary>
    /// Replies to a specific Discord message as the specified character.
    /// </summary>
    public async Task ReplyAsCharacterAsync(
        string characterName,
        ulong channelId,
        ulong messageId,
        string replyContent,
        CancellationToken cancellationToken = default)
    {
        if (!_clients.TryGetValue(characterName, out var client))
        {
            _logger.LogWarning("No client found for character: {Name}", characterName);
            return;
        }

        try
        {
            var channel = await client.GetChannelAsync(channelId);
            var originalMessage = await channel.GetMessageAsync(messageId);

            var builder = new DiscordMessageBuilder()
                .WithContent(replyContent)
                .WithReply(originalMessage.Id, true);

            await channel.SendMessageAsync(builder);
            _logger.LogDebug("Replied as {Character} to message {MessageId}", characterName, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reply as character {Name}", characterName);
        }
    }

    /// <summary>
    /// Shows a typing indicator in the channel as the specified character.
    /// </summary>
    public async Task TriggerTypingAsync(string characterName, ulong channelId)
    {
        if (!_clients.TryGetValue(characterName, out var client))
            return;

        try
        {
            var channel = await client.GetChannelAsync(channelId);
            await channel.TriggerTypingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to trigger typing for {Name}", characterName);
        }
    }

    /// <summary>
    /// Returns whether a client exists for the given character name.
    /// </summary>
    public bool HasCharacter(string characterName) =>
        _clients.ContainsKey(characterName);

    public async ValueTask DisposeAsync()
    {
        foreach (var (name, client) in _clients)
        {
            try
            {
                await client.DisconnectAsync();
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disconnecting character bot {Name}", name);
            }
        }
        _clients.Clear();
    }
}
