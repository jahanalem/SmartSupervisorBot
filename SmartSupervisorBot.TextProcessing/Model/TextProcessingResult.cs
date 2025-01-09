namespace SmartSupervisorBot.TextProcessing.Model
{
    public record TextProcessingResult(string Result, bool groupIsActive, string MessageToUser = null);
}
