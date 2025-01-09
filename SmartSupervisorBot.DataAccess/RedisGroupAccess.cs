using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using SmartSupervisorBot.Model;
using StackExchange.Redis;


namespace SmartSupervisorBot.DataAccess
{
    public class RedisGroupAccess : IGroupAccess
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public RedisGroupAccess(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
        }

        public async Task<bool> AddGroupAsync(long groupId, GroupInfo groupInfo)
        {
            var groupData = JsonConvert.SerializeObject(groupInfo);

            return await _db.StringSetAsync(groupId.ToString(), groupData);
        }

        public async Task<string> GetGroupLanguageAsync(string groupId)
        {
            string cacheKey = $"groupLanguage-{groupId}";
            if (_cache.TryGetValue(cacheKey, out string language))
            {
                return language;
            }
            var groupInfoString = await _db.StringGetAsync(groupId);
            var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
            language = groupInfo.Language;

            _cache.Set(cacheKey, language, TimeSpan.FromDays(30));

            return language;
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

        public async Task UpdateGroupNameAsync(string groupId, string newGroupName)
        {
            var groupInfoString = await _db.StringGetAsync(groupId);
            if (!groupInfoString.IsNullOrEmpty)
            {
                var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
                groupInfo.GroupName = newGroupName;
                var updatedGroupInfoString = JsonConvert.SerializeObject(groupInfo);
                await _db.StringSetAsync(groupId, updatedGroupInfoString);
                Console.WriteLine($"Updated group name to {newGroupName} for group ID: {groupId}");
            }
            else
            {
                Console.WriteLine("Group not found or no name to update.");
            }
        }

        public async Task<bool> SetGroupLanguageAsync(string groupId, string newLanguage)
        {
            var groupInfoString = await _db.StringGetAsync(groupId);
            if (!groupInfoString.IsNullOrEmpty)
            {
                var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
                if (groupInfo.Language != newLanguage)
                {
                    groupInfo.Language = newLanguage;
                    var updatedGroupInfoString = JsonConvert.SerializeObject(groupInfo);
                    await _db.StringSetAsync(groupId, updatedGroupInfoString);
                    _cache.Set($"groupLanguage-{groupId}", newLanguage, TimeSpan.FromDays(30));
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Group not found or no language to update.");
            }
            return false;
        }

        public async Task<bool> SetToggleGroupActive(string groupId, bool isActive)
        {
            var groupInfoString = await _db.StringGetAsync(groupId);
            if (!groupInfoString.IsNullOrEmpty)
            {
                var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
                if (groupInfo.IsActive != isActive)
                {
                    groupInfo.IsActive = isActive;
                    var updatedGroupInfoString = JsonConvert.SerializeObject(groupInfo);
                    await _db.StringSetAsync(groupId, updatedGroupInfoString);
                    _cache.Set($"groupActive-{groupId}", isActive, TimeSpan.FromDays(30));
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Group not found to update.");
            }
            return false;
        }

        public async Task<bool> GroupExistsAsync(string groupId)
        {
            return await _db.KeyExistsAsync(groupId);
        }

        public async Task<bool> IsActivatedGroup(string groupId)
        {
            if (!_cache.TryGetValue(groupId, out bool isActive))
            {
                var groupInfoString = await _db.StringGetAsync(groupId);
                if ((!string.IsNullOrEmpty(groupInfoString)))
                {
                    var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
                    isActive = groupInfo.IsActive;
                    _cache.Set($"groupActive-{groupId}", isActive, TimeSpan.FromDays(30));
                }
            }
            return isActive;
        }

        public async Task<bool> RemoveGroupAsync(string groupId)
        {
            return await _db.KeyDeleteAsync(groupId);
        }

        public List<string> ListAllGroupNames()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "*").Select(key => key.ToString());

            return keys.ToList();
        }

        public async Task<List<(string GroupId, GroupInfo GroupInfo)>> ListAllGroupsWithLanguagesAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys();
            var groupInfos = new List<(string, GroupInfo)>();

            foreach (var key in keys)
            {
                var groupInfoJson = await _db.StringGetAsync(key);
                var groupInfo = JsonConvert.DeserializeObject<GroupInfo>(groupInfoJson) ?? new GroupInfo();
                groupInfos.Add((key.ToString(), groupInfo));
            }

            return groupInfos;
        }

        public async Task<GroupInfo> GetGroupInfoAsync(string groupId)
        {
            var groupInfoString = await _db.StringGetAsync(groupId);
            return JsonConvert.DeserializeObject<GroupInfo>(groupInfoString);
        }

        public async Task UpdateGroupInfoAsync(string groupId, GroupInfo groupInfo)
        {
            var groupInfoString = JsonConvert.SerializeObject(groupInfo);
            await _db.StringSetAsync(groupId, groupInfoString);
        }

        public async Task AddCreditToGroupAsync(string groupId, decimal creditAmount)
        {
            var groupInfo = await GetGroupInfoAsync(groupId);
            groupInfo.CreditPurchased += creditAmount;
            await UpdateGroupInfoAsync(groupId, groupInfo);
        }
    }
}
