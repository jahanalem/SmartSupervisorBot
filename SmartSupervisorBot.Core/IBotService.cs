
using SmartSupervisorBot.Model;

namespace SmartSupervisorBot.Core
{
    public interface IBotService
    {
        void StartReceivingMessages();
        Task AddGroup(long groupId, GroupInfo groupInfo);
        Task<bool> DeleteGroup(string groupId);
        Task<bool> EditLanguage(string groupId, string language);
        Task<List<GroupInfoDto>> ListGroups();
        Task<bool> ToggleGroupActive(string groupId, bool isActive);
        Task AddCreditToGroupAsync(string groupId, decimal creditAmount);
        void Dispose();
    }
}