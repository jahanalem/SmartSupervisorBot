using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model.OpenAI.Chat.Completions
{
    public class ChatMessageResponse
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
