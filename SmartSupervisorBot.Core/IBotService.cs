
namespace SmartSupervisorBot.Core
{
    public interface IBotService
    {
        void StartReceivingMessages();
        Task AddGroup(string groupName, string language);
        Task<bool> DeleteGroup(string groupName);
        Task<bool> EditGroup(string oldGroupName, string newGroupName);
        Task<bool> EditLanguage(string groupName, string language);
        Task<List<(string GroupName, string Language)>> ListGroups();
        void Dispose();
    }
}