using System.Text.Json.Serialization;

namespace ChatRoleplay.Models;

/// <summary>
/// Represents the structured response parsed from the AI output.
/// Supports both tool-calling mode and legacy [CHAR]/[CONTENT] text format.
/// </summary>
public class CharacterResponse
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("emotion")]
    public string? Emotion { get; set; }
}
