using OpenAI_API;
using OpenAI_API.Completions;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;

namespace SmartSupervisorBot.TextProcessing
{
    public class OpenAiTextProcessingService : ITextProcessingService
    {
        private readonly OpenAIAPI _api;
        private readonly IGroupAccess _groupAccess;

        public OpenAiTextProcessingService(string apiKey, IGroupAccess groupAccess)
        {
            _api = new OpenAIAPI(apiKey);
            _groupAccess = groupAccess;
        }

        public async Task<TextProcessingResult> ProcessTextAsync(TextProcessingRequest request)
        {
            var tokenCostData = TokenUtility.CalculateTokenAndCost(request.Prompt, request.MaxTokens, request.Model);

            var groupInfo = await _groupAccess.GetGroupInfoAsync(request.GroupId);
            var newCreditUsed = groupInfo.CreditUsed + tokenCostData.EstimatedCost;

            if (newCreditUsed > groupInfo.CreditPurchased)
            {
                return new TextProcessingResult(null, tokenCostData.EstimatedCost, false, "Your credit has been used up. Please purchase more credit to continue.");
            }

            groupInfo.CreditUsed = newCreditUsed;
            await _groupAccess.UpdateGroupInfoAsync(request.GroupId, groupInfo);


            var completionRequest = new CompletionRequest
            {
                Prompt = request.Prompt,
                Model = request.Model,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };

            var response = await _api.Completions.CreateCompletionAsync(completionRequest);
            var refineResponse = response.ToString().Replace("\n", "").Replace("\r", "").Trim('"', ' ');

            return new TextProcessingResult(refineResponse, tokenCostData.EstimatedCost, true);
        }
    }
}
