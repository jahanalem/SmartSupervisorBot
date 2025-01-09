using Rystem.OpenAi.Chat;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;

namespace SmartSupervisorBot.TextProcessing
{
    // This class is currently not applicable!
    public class RystemTextProcessingService : ITextProcessingService
    {
        private readonly IOpenAiChat _openAiChat;

        public RystemTextProcessingService(IOpenAiChat openAiChat)
        {
            _openAiChat = openAiChat;
        }

        public async Task<TextProcessingResult> ProcessTextAsync(TextProcessingRequest request)
        {
            try
            {
                var chatResponse = await _openAiChat
                    .WithModel("gpt-4o-mini")
                    .AddUserMessage(request.PromptWithMessage)
                    .WithTemperature(request.Temperature)
                    .SetMaxTokens(request.MaxTokens)
                    .ExecuteAsync();

                var result = chatResponse.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

                return new TextProcessingResult(result, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing text with Rystem OpenAi Chat: {ex.Message}");
                throw;
            }
        }
    }
}
