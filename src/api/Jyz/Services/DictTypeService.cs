using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.DictType;
using Jyz.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jyz.Services
{
    public class DictTypeService(AppDbContext context, ReadonlyDBContext<AppDbContext> readonlyDBContext)
        : Service<DictType, AppDbContext>(context, readonlyDBContext), IDictTypeService
    {
        public async Task<PagedData<DictType>> GetPagedAsync(QueryDictTypeRequest query)
        {
            var q = _readonlyContext.DictTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchKey))
                q = q.Where(t => t.Code.Contains(query.SearchKey) || t.Name.Contains(query.SearchKey));

            if (query.Status.HasValue)
                q = q.Where(t => t.Status == query.Status.Value);

            return await q.OrderByDescending(t => t.CreatedTime).ToPagedAsync(query.PageIndex, query.PageSize);
        }

        public async Task<bool> IsCodeDuplicateAsync(string code, int? excludeId = null)
        {
            var q = _readonlyContext.DictTypes.Where(t => t.Code == code);
            if (excludeId.HasValue)
                q = q.Where(t => t.Id != excludeId.Value);
            return await q.AnyAsync();
        }

        public async Task<DictType?> GetByIdWithItemsAsync(int id)
        {
            return await _readonlyContext.DictTypes
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<DictType>> GetAllWithItemsAsync()
        {
            return await _readonlyContext.DictTypes
                .Include(t => t.Items.Where(i => i.Status == 1))
                .Where(t => t.Status == 1)
                .OrderBy(t => t.Code)
                .ToListAsync();
        }
    }
}
