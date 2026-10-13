using System.Text.Json;
using JfYu.Redis.Interface;
using Jyz.Model.RedisCache;
using Jyz.Services.Interfaces;
using AppRedisKey = Jyz.Constants.RedisKey;
using StackExchange.Redis;

namespace Jyz.Services
{
    public class RedisCacheService(IRedisService redisService) : IRedisCacheService
    {
        private const int MaxValueLength = 500;
        private readonly IRedisService _redisService = redisService;

        public async Task<List<RedisCacheItemResponse>> GetAllAsync()
        {
            var pattern = $"{AppRedisKey.ApplicationName}:*";
            var db = _redisService.Database;
            var result = new List<RedisCacheItemResponse>();

            // Iterate all endpoints to support single-node, Sentinel, and Cluster configurations
            foreach (var endpoint in _redisService.Client.GetEndPoints())
            {
                var server = _redisService.Client.GetServer(endpoint);
                await foreach (var key in server.KeysAsync(pattern: pattern))
                    result.Add(await BuildCacheItemAsync(db, key));
            }

            return result;
        }

        private static async Task<RedisCacheItemResponse> BuildCacheItemAsync(IDatabase db, StackExchange.Redis.RedisKey key)
        {
            var ttlSpan = await db.KeyTimeToLiveAsync(key);
            long ttl = ttlSpan.HasValue ? (long)ttlSpan.Value.TotalSeconds : -1;
            var (rawValue, isJson) = await ReadValueAsync(db, key);

            return new RedisCacheItemResponse
            {
                Key = key.ToString(),
                Ttl = ttl,
                Value = rawValue,
                IsJson = isJson,
            };
        }

        private static async Task<(string? Value, bool IsJson)> ReadValueAsync(IDatabase db, StackExchange.Redis.RedisKey key)
        {
            try
            {
                var redisValue = await db.StringGetAsync(key);
                if (!redisValue.HasValue)
                    return (null, false);

                var rawValue = redisValue.ToString();
                if (rawValue == null)
                    return (null, false);

                // Validate JSON before truncating so long JSON is correctly identified
                var isJson = IsValidJson(rawValue);
                if (rawValue.Length > MaxValueLength)
                    rawValue = rawValue[..MaxValueLength] + "...";

                return (rawValue, isJson);
            }
            catch
            {
                return ("(non-string type)", false);
            }
        }

        public async Task DeleteAsync(List<string> keys)
        {
            var db = _redisService.Database;
            var redisKeys = keys.Select(k => (StackExchange.Redis.RedisKey)k).ToArray();
            await db.KeyDeleteAsync(redisKeys);
        }

        private static bool IsValidJson(string value)
        {
            var trimmed = value.TrimStart();
            if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
                return false;
            try
            {
                JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
