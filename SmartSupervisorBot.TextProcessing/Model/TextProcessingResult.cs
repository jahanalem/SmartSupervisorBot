namespace SmartSupervisorBot.TextProcessing.Model
{
    public record TextProcessingResult(
        string Result,
        decimal EstimatedCost,
        bool IsCreditSufficient,
        string UserMessage = null);
}
