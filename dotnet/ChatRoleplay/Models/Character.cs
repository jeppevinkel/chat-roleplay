using System.Text.Json.Serialization;

namespace ChatRoleplay.Models;

public class Character
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("longDescription")]
    public string LongDescription { get; set; } = string.Empty;

    [JsonPropertyName("personality")]
    public string Personality { get; set; } = string.Empty;

    [JsonPropertyName("botToken")]
    public string BotToken { get; set; } = string.Empty;
}
