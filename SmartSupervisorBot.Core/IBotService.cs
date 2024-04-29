
namespace SmartSupervisorBot.Core
{
    public interface IBotService
    {
        void StartReceivingMessages();
        Task AddGroup(string groupName);
        Task<bool> DeleteGroup(string groupName);
        Task EditGroup(string oldGroupName, string newGroupName);
        Task<List<string>> ListGroups();
        void Dispose();
    }
}