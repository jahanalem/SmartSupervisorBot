namespace SmartSupervisorBot.Core.Settings
{
    public class BotConfigurationOptions
    {
        public BotSettings BotSettings { get; set; }
        public string OpenAiModel { get; set; }
        public string TranslateTheTextTo { get; set; }
        public AllowedUpdatesSettings AllowedUpdatesSettings { get; set; }
        public UnifiedTextSettings UnifiedTextSettings { get; set; }
    }
}
