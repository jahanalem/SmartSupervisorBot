using SmartSupervisorBot.Model;

namespace SmartSupervisorBot.DataAccess
{
    public interface IGroupAccess
    {
        Task<bool> IsActivatedGroup(string groupId);
        Task<bool> SetToggleGroupActive(string groupId, bool isActive);
        Task<bool> AddGroupAsync(long groupId, GroupInfo groupInfo);
        Task<string> GetGroupLanguageAsync(string groupId);
        Task<bool> SetGroupLanguageAsync(string groupId, string newLanguage);
        Task<bool> RemoveGroupAsync(string groupId);
        Task<bool> GroupExistsAsync(string groupId);
        List<string> ListAllGroupNames();
        Task<List<(string GroupId, GroupInfo GroupInfo)>> ListAllGroupsWithLanguagesAsync();
        Task UpdateGroupNameAsync(string groupId, string newGroupName);
        Task<GroupInfo> GetGroupInfoAsync(string groupId);
        Task UpdateGroupInfoAsync(string groupId, GroupInfo groupInfo);
        Task AddCreditToGroupAsync(string groupId, decimal creditAmount);
    }
}
