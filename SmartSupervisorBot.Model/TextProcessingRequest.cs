namespace SmartSupervisorBot.Model
{
    public record TextProcessingRequest(string Prompt, string PromptWithMessage, string Model, int MaxTokens, double Temperature, string GroupId, string UserMessage);
}
