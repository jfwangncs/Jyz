using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.DictItem;
using Jyz.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jyz.Services
{
    public class DictItemService(AppDbContext context, ReadonlyDBContext<AppDbContext> readonlyDBContext)
        : Service<DictItem, AppDbContext>(context, readonlyDBContext), IDictItemService
    {
        public async Task<PagedData<DictItem>> GetPagedAsync(QueryDictItemRequest query)
        {
            var q = _readonlyContext.DictItems.AsQueryable();

            if (query.DictTypeId.HasValue)
                q = q.Where(i => i.DictTypeId == query.DictTypeId.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchKey))
                q = q.Where(i => i.Code.Contains(query.SearchKey) || i.Label.Contains(query.SearchKey));

            if (query.Status.HasValue)
                q = q.Where(i => i.Status == query.Status.Value);

            return await q.OrderBy(i => i.Sort).ThenBy(i => i.CreatedTime).ToPagedAsync(query.PageIndex, query.PageSize);
        }

        public async Task<bool> IsCodeDuplicateAsync(int dictTypeId, string code, int? excludeId = null)
        {
            var q = _readonlyContext.DictItems.Where(i => i.DictTypeId == dictTypeId && i.Code == code);
            if (excludeId.HasValue)
                q = q.Where(i => i.Id != excludeId.Value);
            return await q.AnyAsync();
        }
    }
}
