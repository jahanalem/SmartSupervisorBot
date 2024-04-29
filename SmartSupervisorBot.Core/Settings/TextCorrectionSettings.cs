namespace SmartSupervisorBot.Core.Settings
{
    public class TextCorrectionSettings
    {
        public string Model { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public string Prompt { get; set; }
    }
}
