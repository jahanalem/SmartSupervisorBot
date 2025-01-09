namespace SmartSupervisorBot.Core.Settings
{
    public abstract class BaseOpenAiTextSettings : IBaseOpenAiTextSettings
    {
        public virtual int MaxTokens { get; set; }
        public virtual double Temperature { get; set; }
        public virtual string Prompt { get; set; }
    }
}
