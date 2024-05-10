using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupervisorBot.Model
{
    public class GroupInfo
    {
        public string GroupName { get; set; }
        public string Language { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        public GroupInfo()
        {
            CreatedDate = DateTime.UtcNow;
            IsActive = false;
            Language = "Deutsch";
        }
    }
}
