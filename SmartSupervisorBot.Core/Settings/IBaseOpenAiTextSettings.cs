using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
