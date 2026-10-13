using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.DictItem;

namespace Jyz.Services.Interfaces
{
    public interface IDictItemService : IService<DictItem, AppDbContext>
    {
        Task<PagedData<DictItem>> GetPagedAsync(QueryDictItemRequest query);
        Task<bool> IsCodeDuplicateAsync(int dictTypeId, string code, int? excludeId = null);
    }
}
