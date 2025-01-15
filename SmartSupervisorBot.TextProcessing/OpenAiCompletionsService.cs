using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.Model.OpenAI.Completions;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;
using System.Net.Http.Json;

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
            // Validate and update group credits
            var groupInfo = await ValidateAndUpdateCreditsAsync(request);
            if (!groupInfo.IsActive)
            {
                return new TextProcessingResult(null, false, "Your credit has been used up. Please purchase more credit to continue.");
            }

            // Build the request body for the OpenAI /completions endpoint
            var body = new CompletionRequest
            {
                Model = request.Model,
                Prompt = request.PromptWithMessage,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };

            try
            {
                // Make the POST request to OpenAI /v1/completions
                var completionResponse = await SendCompletionRequestAsync(body);

                // Extract and refine the text response
                var text = ExtractResponseText(completionResponse);

                return new TextProcessingResult(text, groupInfo.IsActive);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception occurred in {nameof(ProcessTextAsync)}: {ex}");
                throw;
            }
        }

        private async Task<GroupInfo> ValidateAndUpdateCreditsAsync(TextProcessingRequest request)
        {
            var tokenCostData = TokenUtility.CalculateTokenAndCost(request.PromptWithMessage, request.MaxTokens, request.Model);
            var groupInfo = await _groupAccess.GetGroupInfoAsync(request.GroupId);

            var newCreditUsed = groupInfo.CreditUsed + tokenCostData.EstimatedCost;

            if (newCreditUsed > groupInfo.CreditPurchased)
            {
                groupInfo.IsActive = false;
                await _groupAccess.SetToggleGroupActive(request.GroupId, false);
                return groupInfo;
            }

            groupInfo.CreditUsed = newCreditUsed;
            await _groupAccess.UpdateGroupInfoAsync(request.GroupId, groupInfo);

            return groupInfo;
        }

        private async Task<CompletionResponse> SendCompletionRequestAsync(CompletionRequest body)
        {
            var httpResponse = await HttpClient.PostAsJsonAsync(
                "completions",
                body,
                OpenAiJsonSerializerContext.Default.CompletionRequest
            );

            return await DeserializeResponseAsync<CompletionResponse>(httpResponse);
        }

        private static string ExtractResponseText(CompletionResponse completionResponse)
        {
            var text = completionResponse?.Choices?.FirstOrDefault()?.Text ?? string.Empty;
            return text.Replace("\n", "").Replace("\r", "").Trim('"', ' ');
        }
    }
}