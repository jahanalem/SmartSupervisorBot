using System.Text.Json.Serialization;

namespace SmartSupervisorBot.Model
{
    public class GroupInfo
    {
        [JsonPropertyName("group_name")]
        public string GroupName { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("credit_purchased")]
        public decimal CreditPurchased { get; set; }

        [JsonPropertyName("credit_used")]
        public decimal CreditUsed { get; set; }

        [JsonPropertyName("created_date")]
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
