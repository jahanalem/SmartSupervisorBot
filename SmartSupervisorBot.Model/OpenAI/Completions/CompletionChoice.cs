using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model.OpenAI.Completions
{
    public class CompletionChoice
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }
}
