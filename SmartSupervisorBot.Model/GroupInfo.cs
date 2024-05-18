namespace SmartSupervisorBot.Model
{
    public class GroupInfo
    {
        public string GroupName { get; set; }
        public string Language { get; set; }
        public bool IsActive { get; set; }
        public decimal CreditPurchased { get; set; }
        public decimal CreditUsed { get; set; }
        public DateTime CreatedDate { get; set; }

        public GroupInfo()
        {
            CreatedDate = DateTime.UtcNow;
            IsActive = false;
            Language = "Deutsch";
            CreditPurchased = 0;
            CreditUsed = 0;
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

            if (CreditPurchased < 0)
            {
                throw new ArgumentException("Credit purchased cannot be negative.", nameof(CreditPurchased));
            }

            if (CreditUsed < 0)
            {
                throw new ArgumentException("Credit used cannot be negative.", nameof(CreditUsed));
            }

            if (CreditUsed > CreditPurchased)
            {
                throw new InvalidOperationException("Credit used cannot exceed credit purchased.");
            }
        }
    }
}
