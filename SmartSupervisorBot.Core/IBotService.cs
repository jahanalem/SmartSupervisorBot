
using SmartSupervisorBot.Model;

namespace SmartSupervisorBot.Core
{
    public interface IBotService
    {
        void StartReceivingMessages();
        Task AddGroup(long groupId, GroupInfo groupInfo);
        Task<bool> DeleteGroup(string groupName);
        Task<bool> EditLanguage(string groupName, string language);
        Task<List<(string GroupId, GroupInfo GroupInfo)>> ListGroups();
        Task<bool> ToggleGroupActive(string groupId, bool isActive);
        void Dispose();
    }
}