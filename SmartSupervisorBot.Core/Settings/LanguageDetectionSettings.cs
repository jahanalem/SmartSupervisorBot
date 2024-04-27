using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.Core.Settings
{
    public class LanguageDetectionSettings
    {
        public string Model { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }
}
