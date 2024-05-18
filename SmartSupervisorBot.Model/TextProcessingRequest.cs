namespace SmartSupervisorBot.Model
{
    public record TextProcessingRequest(
        string Prompt,
        string Model,
        int MaxTokens,
        double Temperature,
        string GroupId);
}
