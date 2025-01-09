using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SmartSupervisorBot.TextProcessing
{
    public class OpenAiCompletionsService : OpenAiBaseService, ITextProcessingService
    {
        private readonly IGroupAccess _groupAccess;

        public OpenAiCompletionsService(string apiKey, IGroupAccess groupAccess) : base(apiKey)
        {
            _groupAccess = groupAccess;
        }

        public async Task<TextProcessingResult> ProcessTextAsync(TextProcessingRequest request)
        {
            // Calculate tokens/cost
            var tokenCostData = TokenUtility.CalculateTokenAndCost(request.PromptWithMessage, request.MaxTokens, request.Model);

            // Check/update group credits
            var groupInfo = await _groupAccess.GetGroupInfoAsync(request.GroupId);
            var newCreditUsed = groupInfo.CreditUsed + tokenCostData.EstimatedCost;

            if (newCreditUsed > groupInfo.CreditPurchased)
            {
                groupInfo.IsActive = false;
                await _groupAccess.SetToggleGroupActive(request.GroupId, false);

                return new TextProcessingResult(Result: null, false, MessageToUser: "Your credit has been used up. Please purchase more credit to continue.");
            }

            groupInfo.CreditUsed = newCreditUsed;
            await _groupAccess.UpdateGroupInfoAsync(request.GroupId, groupInfo);

            // Build the request body for the OpenAI /completions endpoint
            var body = new
            {
                model = request.Model,
                prompt = request.PromptWithMessage,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            // Make the POST request to OpenAI /v1/completions
            var httpResponse = await HttpClient.PostAsJsonAsync("completions", body);

            // Parse the JSON response into CompletionResponse
            var completionResponse = await DeserializeResponseAsync<CompletionResponse>(httpResponse);

            // Extract the text from the first choice
            var text = completionResponse?.Choices?.Count > 0 ? completionResponse.Choices[0].Text : string.Empty;

            // Refine the response (remove newlines, quotes, etc.)
            var refineResponse = text.Replace("\n", "").Replace("\r", "").Trim('"', ' ');

            // Return your custom TextProcessingResult
            return new TextProcessingResult(refineResponse, groupInfo.IsActive);
        }
    }

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
