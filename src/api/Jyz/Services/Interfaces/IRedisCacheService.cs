using Jyz.Model.RedisCache;

namespace Jyz.Services.Interfaces
{
    public interface IRedisCacheService
    {
        Task<List<RedisCacheItemResponse>> GetAllAsync();
        Task DeleteAsync(List<string> keys);
    }
}
