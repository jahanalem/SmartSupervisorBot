namespace SmartSupervisorBot.DataAccess
{
    public interface IGroupAccess
    {
        Task<bool> AddGroupAsync(string groupName);
        Task<bool> RemoveGroupAsync(string groupName);
        Task<bool> GroupExistsAsync(string groupName);
        Task<List<string>> ListAllGroupsAsync();
    }
}
