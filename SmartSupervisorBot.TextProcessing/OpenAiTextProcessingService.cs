using OpenAI_API;
using OpenAI_API.Completions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.TextProcessing
{
    public class OpenAiTextProcessingService : ITextProcessingService
    {
        private readonly OpenAIAPI _api;

        public OpenAiTextProcessingService(string apiKey)
        {
            _api = new OpenAIAPI(apiKey);
        }
        public async Task<string> ProcessTextAsync(string prompt, string model, int maxTokens, double temperature)
        {
            var completionRequest = new CompletionRequest
            {
                Prompt = prompt,
                Model = model,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var response = await _api.Completions.CreateCompletionAsync(completionRequest);
            return response.ToString().Replace("\n", "").Replace("\r", "").Trim('"', ' ');
        }
    }
}
