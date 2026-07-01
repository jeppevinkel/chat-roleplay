using System.Text.Json.Serialization;

namespace ChatRoleplay.Config;

public class ManagerBotConfig
{
    [JsonPropertyName("botToken")]
    public string BotToken { get; set; } = string.Empty;
}

public class CoreConfig
{
    [JsonPropertyName("managerBot")]
    public ManagerBotConfig ManagerBot { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o";

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "openai"; // "openai" or "ollama"

    [JsonPropertyName("customEndpoint")]
    public string? CustomEndpoint { get; set; }

    [JsonPropertyName("aiToken")]
    public string AiToken { get; set; } = string.Empty;

    [JsonPropertyName("idleIntervalSec")]
    public int IdleIntervalSec { get; set; } = 180;

    [JsonPropertyName("roleplayChannels")]
    public List<ulong> RoleplayChannels { get; set; } = [];

    [JsonPropertyName("useToolCalling")]
    public bool UseToolCalling { get; set; } = true;
}
