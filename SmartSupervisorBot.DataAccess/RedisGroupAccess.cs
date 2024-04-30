using StackExchange.Redis;

namespace SmartSupervisorBot.DataAccess
{
    public class RedisGroupAccess : IGroupAccess
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisGroupAccess(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
        }

        public async Task<bool> AddGroupAsync(string groupName, string language)
        {
            return await _db.StringSetAsync(groupName, language);
        }

        public async Task<string> GetGroupLanguageAsync(string groupName)
        {
            return await _db.StringGetAsync(groupName);
        }

        public async Task<bool> RenameGroupAsync(string oldGroupName, string newGroupName)
        {
            var language = await _db.StringGetAsync(oldGroupName);
            if (!language.IsNullOrEmpty)
            {
                await _db.KeyDeleteAsync(oldGroupName);
                return await _db.StringSetAsync(newGroupName, language);
            }
            return false;
        }

        public async Task<bool> SetGroupLanguageAsync(string groupName, string newLanguage)
        {
            return await _db.StringSetAsync(groupName, newLanguage);
        }

        public async Task<bool> GroupExistsAsync(string groupName)
        {
            return await _db.KeyExistsAsync(groupName);
        }

        public async Task<bool> RemoveGroupAsync(string groupName)
        {
            return await _db.KeyDeleteAsync(groupName);
        }

        public async Task<List<string>> ListAllGroupNamesAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "*").Select(key => key.ToString());

            return keys.ToList();
        }

        public async Task<List<(string GroupName, string Language)>> ListAllGroupsWithLanguagesAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys();
            var groupsWithLanguages = new List<(string, string)>();

            foreach (var key in keys)
            {
                var language = await _db.StringGetAsync(key);
                groupsWithLanguages.Add((key.ToString(), language));
            }

            return groupsWithLanguages;
        }
    }
}
