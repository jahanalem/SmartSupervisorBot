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

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                throw new ArgumentException("Group name must not be null or empty.", nameof(GroupName));
            }

            if (GroupName.Length > 255)
            {
                throw new ArgumentException("Group name must not exceed 255 characters.", nameof(GroupName));
            }

            if (string.IsNullOrWhiteSpace(Language))
            {
                throw new ArgumentException("Language must not be null or empty.", nameof(Language));
            }
        }
    }
}
