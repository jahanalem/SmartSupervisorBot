using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing.Model;

namespace SmartSupervisorBot.TextProcessing
{
    public interface ITextProcessingService
    {
        Task<TextProcessingResult> ProcessTextAsync(TextProcessingRequest request);
    }
}
