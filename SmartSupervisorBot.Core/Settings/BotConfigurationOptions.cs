namespace SmartSupervisorBot.Core.Settings
{
    public class BotConfigurationOptions
    {
        public BotSettings BotSettings { get; set; }
        public string TranslateTheTextTo { get; set; }
        public LanguageDetectionSettings LanguageDetectionSettings { get; set; }
        public TextCorrectionSettings TextCorrectionSettings { get; set; }
        public TextTranslateSettings TextTranslateSettings { get; set; }
        public AllowedUpdatesSettings AllowedUpdatesSettings { get; set; }
    }
}
