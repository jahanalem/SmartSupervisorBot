using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model.OpenAI.Completions
{
    // Minimal classes to parse the /completions JSON response
    public class CompletionResponse
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
        public List<CompletionChoice> Choices { get; set; }
    }
}
