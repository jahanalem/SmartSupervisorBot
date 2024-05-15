using OpenAI_API.Completions;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.TextProcessing
{
    public class RystemTextProcessingService : ITextProcessingService
    {
        private readonly IOpenAiChat _openAiChat;

        public RystemTextProcessingService(IOpenAiChat openAiChat)
        {
            _openAiChat = openAiChat;
        }


        public async Task<string> ProcessTextAsync(string prompt, string model, int maxTokens, double temperature)
        {
            var chatRequestBuilder = _openAiChat.RequestWithUserMessage(prompt)
                .WithModel(model).WithTemperature(temperature).SetMaxTokens(maxTokens);

            var chatResponse = await chatRequestBuilder.ExecuteAsync();

            var result = chatResponse.Choices.FirstOrDefault()?.Message.Content.ToString();


            return result;
        }
    }
}
