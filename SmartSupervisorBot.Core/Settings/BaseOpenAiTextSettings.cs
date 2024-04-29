using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.Core.Settings
{
    public abstract class BaseOpenAiTextSettings : IBaseOpenAiTextSettings
    {
        public virtual string Model { get; set; }
        public virtual int MaxTokens { get; set; }
        public virtual double Temperature { get; set; }
        public virtual string Prompt { get; set; }
    }
}
