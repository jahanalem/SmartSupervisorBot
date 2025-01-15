using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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

            var stream = await response.Content.ReadAsStreamAsync();
            var options = OpenAiJsonSerializerContext.Default.GetTypeInfo(typeof(T));

            return (T)JsonSerializer.Deserialize(stream, (JsonTypeInfo<T>)options)!;
        }
    }
}
