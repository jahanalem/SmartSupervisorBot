using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;

namespace SmartSupervisorBot.TextProcessing
{
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
                var chatRequestBuilder = _openAiChat.RequestWithUserMessage(request.Prompt)
                                                    .WithModel(ChatModelType.Gpt35Turbo)
                                                    .WithTemperature(request.Temperature)
                                                    .SetMaxTokens(request.MaxTokens);

                var chatResponse = await chatRequestBuilder.ExecuteAsync();
                var result = chatResponse.Choices.FirstOrDefault()?.Message.Content.ToString();

                var refineResponse = result;

                return new TextProcessingResult(refineResponse, 0, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing text with OpenAi Chat.");
                throw;
            }
        }
    }
}
