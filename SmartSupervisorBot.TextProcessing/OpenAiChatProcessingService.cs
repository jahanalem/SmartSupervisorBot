using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

// Role definitions in ChatGPT API
// https://www.baeldung.com/cs/chatgpt-api-roles

// General guide on text generation and completion endpoints
// https://platform.openai.com/docs/guides/text-generation

// Compatibility details for models and endpoints
// https://platform.openai.com/docs/models#model-endpoint-compatibility

// Model ID aliases and versioning
// https://platform.openai.com/docs/models#model-id-aliases-and-snapshots

// Full API reference for chat endpoints
// https://platform.openai.com/docs/api-reference/chat

namespace SmartSupervisorBot.TextProcessing
{
    public class OpenAiChatProcessingService : OpenAiBaseService, ITextProcessingService
    {
        private readonly IGroupAccess _groupAccess;

        public OpenAiChatProcessingService(string apiKey, IGroupAccess groupAccess) : base(apiKey)
        {
            _groupAccess = groupAccess;
        }

        public async Task<TextProcessingResult> ProcessTextAsync(TextProcessingRequest request)
        {
            var groupInfo = await _groupAccess.GetGroupInfoAsync(request.GroupId);

            if (!groupInfo.IsActive)
            {
                return new TextProcessingResult(null, false, "Your group has been deactivated. This may be due to insufficient credit. Please contact the bot administrator for assistance.");
            }

            // Build the request for /v1/chat/completions
            var body = new
            {
                model = request.Model,
                messages = new[]
                {
                    new { role = "system", content = request.Prompt },
                    new { role = "user", content = request.UserMessage }
                },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            var response = await HttpClient.PostAsJsonAsync("chat/completions", body);

            var chatResponse = await DeserializeResponseAsync<ChatCompletionResponse>(response);

            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            var promptTokens = chatResponse?.Usage?.PromptTokens ?? 0;
            var completionTokens = chatResponse?.Usage?.CompletionTokens ?? 0;
            var totalCost = OpenAiCostCalculator.CalculateCost(promptTokens, completionTokens, request.Model);

            var messageToUser = string.Empty;

            // Update group credits and status
            groupInfo.CreditUsed += totalCost;
            if (groupInfo.CreditUsed >= groupInfo.CreditPurchased)
            {
                groupInfo.IsActive = false;
                messageToUser = "<i> Your group's credit has been depleted. Please recharge your account to continue using the bot. For assistance, contact the bot administrator: @Roohi_C </i>";
            }
            await _groupAccess.UpdateGroupInfoAsync(request.GroupId, groupInfo);

            return new TextProcessingResult(content.Trim(), groupInfo.IsActive, messageToUser);
        }
    }

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

    public class ChatCompletionChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatMessageResponse Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class ChatMessageResponse
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class TokenUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
