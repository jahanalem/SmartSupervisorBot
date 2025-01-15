using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model.OpenAI.Chat.Completions
{
    // Minimal classes to parse the /chat/completions JSON response
    public class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public TokenUsage Usage { get; set; }
    }

}
