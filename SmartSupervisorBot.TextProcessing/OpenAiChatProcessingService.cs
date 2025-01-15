using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.Model.OpenAI.Chat.Completions;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;
using System.Text;
using System.Text.Json;
using ChatMessage = SmartSupervisorBot.Model.OpenAI.Chat.Completions.ChatMessage;

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
            var groupInfo = await ValidateGroupStatusAsync(request.GroupId);
            if (!groupInfo.IsActive)
            {
                return new TextProcessingResult(string.Empty, false, "Your group has been deactivated. This may be due to insufficient credit. Please contact the bot administrator for assistance.");
            }

            var body = BuildChatCompletionRequest(request);

            var responseContent = await SendChatCompletionRequestAsync(body);

            var responseText = responseContent?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            var messageToUser = await UpdateGroupCreditsAsync(groupInfo, responseContent.Usage, request.Model);

            return new TextProcessingResult(responseText.Trim(), groupInfo.IsActive, messageToUser);
        }

        private async Task<GroupInfo> ValidateGroupStatusAsync(string groupId)
        {
            var groupInfo = await _groupAccess.GetGroupInfoAsync(groupId);
            if (!groupInfo.IsActive)
            {
                groupInfo.IsActive = false;
                await _groupAccess.UpdateGroupInfoAsync(groupId, groupInfo);
            }
            return groupInfo;
        }

        private ChatCompletionRequest BuildChatCompletionRequest(TextProcessingRequest request)
        {
            return new ChatCompletionRequest
            {
                Model = request.Model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = request.Prompt },
                    new ChatMessage { Role = "user", Content = request.UserMessage }
                },
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };
        }

        private async Task<ChatCompletionResponse?> SendChatCompletionRequestAsync(ChatCompletionRequest body)
        {
            var json = JsonSerializer.Serialize(body, OpenAiJsonSerializerContext.Default.ChatCompletionRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync("chat/completions", content);

            return JsonSerializer.Deserialize<ChatCompletionResponse>(await response.Content.ReadAsStreamAsync(), OpenAiJsonSerializerContext.Default.ChatCompletionResponse);
        }

        private async Task<string> UpdateGroupCreditsAsync(GroupInfo groupInfo, TokenUsage usage, string model)
        {
            var promptTokens = usage?.PromptTokens ?? 0;
            var completionTokens = usage?.CompletionTokens ?? 0;
            var totalCost = OpenAiCostCalculator.CalculateCost(promptTokens, completionTokens, model);

            groupInfo.CreditUsed += totalCost;
            if (groupInfo.CreditUsed >= groupInfo.CreditPurchased)
            {
                groupInfo.IsActive = false;
                await _groupAccess.SetToggleGroupActive(groupInfo.GroupName, false);

                return "<i> Your group's credit has been depleted. Please recharge your account to continue using the bot. For assistance, contact the bot administrator: @Roohi_C </i>";
            }

            await _groupAccess.UpdateGroupInfoAsync(groupInfo.GroupName, groupInfo);

            return string.Empty;
        }
    }
}