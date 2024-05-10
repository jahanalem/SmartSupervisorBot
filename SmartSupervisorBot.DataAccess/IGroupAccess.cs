using SmartSupervisorBot.Model;
using System.Text.RegularExpressions;

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
        Task<List<string>> ListAllGroupNamesAsync();
        Task<List<(string GroupId, GroupInfo GroupInfo)>> ListAllGroupsWithLanguagesAsync();
        Task UpdateGroupNameAsync(string groupId, string newGroupName);
    }
}
