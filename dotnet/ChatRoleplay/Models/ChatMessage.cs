using System.Text.Json.Serialization;

namespace ChatRoleplay.Models;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // "system", "user", "assistant"

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    public ChatMessage() { }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}
