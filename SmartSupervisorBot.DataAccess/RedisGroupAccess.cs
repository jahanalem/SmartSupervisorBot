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

        public async Task<bool> AddGroupAsync(string groupName)
        {
            return await _db.StringSetAsync(groupName, true);
        }

        public async Task<bool> GroupExistsAsync(string groupName)
        {
            return await _db.KeyExistsAsync(groupName);
        }

        public async Task<bool> RemoveGroupAsync(string groupName)
        {
            return await _db.KeyDeleteAsync(groupName);
        }

        public async Task<List<string>> ListAllGroupsAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "*").Select(key => key.ToString());

            return keys.ToList();
        }
    }
}
