namespace SmartSupervisorBot.Core.Settings
{
    public interface IBaseOpenAiTextSettings
    {
        int MaxTokens { get; set; }
        double Temperature { get; set; }
        string Prompt { get; set; }
    }
}
