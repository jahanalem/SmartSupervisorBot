namespace SmartSupervisorBot.DataAccess
{
    public interface IGroupAccess
    {
        Task<bool> AddGroupAsync(string groupName, string language);
        Task<string> GetGroupLanguageAsync(string groupName);
        Task<bool> RenameGroupAsync(string oldGroupName, string newGroupName);
        Task<bool> SetGroupLanguageAsync(string groupName, string newLanguage);
        Task<bool> RemoveGroupAsync(string groupName);
        Task<bool> GroupExistsAsync(string groupName);
        Task<List<string>> ListAllGroupNamesAsync();
        Task<List<(string GroupName, string Language)>> ListAllGroupsWithLanguagesAsync();
    }
}
