using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.DictType;

namespace Jyz.Services.Interfaces
{
    public interface IDictTypeService : IService<DictType, AppDbContext>
    {
        Task<PagedData<DictType>> GetPagedAsync(QueryDictTypeRequest query);
        Task<bool> IsCodeDuplicateAsync(string code, int? excludeId = null);
        Task<DictType?> GetByIdWithItemsAsync(int id);
        Task<List<DictType>> GetAllWithItemsAsync();
    }
}
