using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SmartSupervisorBot.TextProcessing
{
    public abstract class OpenAiBaseService
    {
        protected readonly HttpClient HttpClient;

        protected OpenAiBaseService(string apiKey)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        protected async Task<string> HandleErrorResponse(HttpResponseMessage response)
        {
            string errorDetails = await response.Content.ReadAsStringAsync();

            return $"OpenAI API error: {errorDetails}";
        }

        protected async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await HandleErrorResponse(response));
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
    }
}
