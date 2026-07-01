using System.Text.Json.Serialization;
using ChatRoleplay.Models;

namespace ChatRoleplay.Config;

public class PromptConfig
{
    [JsonPropertyName("template")]
    public List<ChatMessage> Template { get; set; } =
    [
        new ChatMessage
        {
            Role = "system",
            Content = "You will respond as one of {num_chars} characters: {chars}. Always respond using a [CHAR] response, which includes only the name of one character at a time, followed by [CONTENT] where the character speaks. The format is: [CHAR] CharacterName [CONTENT] What the character says. For example: [CHAR] Monika [CONTENT] Hello, everyone! Each character has a distinct personality: {chars_personality}. If I use [NAME] and [MSG], you should respond as if the character is speaking to the person named in [NAME], but always use the [CHAR] [CONTENT] format in your response. If the [NAME] is unusual, like \"<@1234567890>\", refer to them exactly as typed. When I type '{RST}', reset the story and start fresh with a [CHAR] response. Only one character should speak at a time, and you should never include more than one [CHAR] response in a single output. If you understand, respond with a [CHAR] type response."
        },
        new ChatMessage
        {
            Role = "user",
            Content = "continue"
        },
        new ChatMessage
        {
            Role = "assistant",
            Content = "[CHAR] Monika [CONTENT] Hello everyone! Welcome to the Literature Club! I'm Monika, the president. I'm so glad you could join us today. We're going to have a wonderful time discussing literature and getting to know each other. Sayori, would you like to start by introducing yourself?"
        },
        new ChatMessage
        {
            Role = "user",
            Content = "{RST}"
        }
    ];

    /// <summary>
    /// Returns a copy of the template with placeholders replaced by actual character information.
    /// </summary>
    public List<ChatMessage> GetFormattedTemplate(List<Character> characters)
    {
        var template = Template
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();

        if (template.Count > 0)
        {
            var content = template[0].Content;
            content = content.Replace("{num_chars}", characters.Count.ToString());
            content = content.Replace("{chars}", string.Join(", ", characters.Select(c => c.Name)));
            content = content.Replace("{chars_personality}", string.Join(". ", characters.Select(c => c.Personality)));
            template[0].Content = content;
        }

        return template;
    }
}
