namespace Jyz.Model.RedisCache
{
    public class RedisCacheItemResponse
    {
        public string Key { get; set; } = string.Empty;
        /// <summary>-1 = no expiry, >0 = seconds remaining</summary>
        public long Ttl { get; set; }
        public string? Value { get; set; }
        public bool IsJson { get; set; }
    }

    public class DeleteRedisCacheRequest
    {
        public List<string> Keys { get; set; } = [];
    }
}

