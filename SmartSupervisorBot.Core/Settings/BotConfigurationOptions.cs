using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.Core.Settings
{
    public class BotConfigurationOptions
    {
        public TextCorrectionSettings TextCorrectionSettings { get; set; }
        public LanguageDetectionSettings LanguageDetectionSettings { get; set; }
        public BotSettings BotSettings { get; set; }
    }
}
