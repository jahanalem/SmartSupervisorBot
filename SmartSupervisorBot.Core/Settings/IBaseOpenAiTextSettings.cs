namespace SmartSupervisorBot.Core.Settings
{
    public interface IBaseOpenAiTextSettings
    {
        string Model { get; set; }
        int MaxTokens { get; set; }
        double Temperature { get; set; }
        string Prompt { get; set; }
    }
}
