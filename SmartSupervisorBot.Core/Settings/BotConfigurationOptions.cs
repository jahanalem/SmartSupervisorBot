namespace SmartSupervisorBot.Core.Settings
{
    public class BotConfigurationOptions
    {
        public TextCorrectionSettings TextCorrectionSettings { get; set; }
        public LanguageDetectionSettings LanguageDetectionSettings { get; set; }
        public BotSettings BotSettings { get; set; }
        public AllowedUpdatesSettings AllowedUpdatesSettings { get; set; }
    }
}
