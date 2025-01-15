using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model.OpenAI.Chat.Completions
{
    public class ChatCompletionChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatMessageResponse Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }
}
