namespace SmartSupervisorBot.TextProcessing
{
    public interface ITextProcessingService
    {
        Task<string> ProcessTextAsync(string prompt, string model, int maxTokens, double temperature);
    }
}
