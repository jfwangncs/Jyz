using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.LoginLog;
using Jyz.Services.Interfaces;

namespace Jyz.Services
{
    public class LoginLogService(AppDbContext context, ReadonlyDBContext<AppDbContext> readonlyDBContext)
        : Service<LoginLog, AppDbContext>(context, readonlyDBContext), ILoginLogService
    {
        public async Task<PagedData<LoginLog>> GetPagedAsync(QueryLoginLogRequest query)
        {
            var q = _readonlyContext.LoginLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchKey))
                q = q.Where(a => a.UserName != null && (a.UserName.Contains(query.SearchKey) || (a.IP != null && a.IP.Contains(query.SearchKey))));

            if (query.Result.HasValue)
                q = q.Where(a => a.Result == query.Result.Value);

            if (query.Platform.HasValue)
                q = q.Where(a => a.Platform == query.Platform.Value);

            if (query.StartTime.HasValue)
                q = q.Where(a => a.CreatedTime >= query.StartTime.Value);

            if (query.EndTime.HasValue)
                q = q.Where(a => a.CreatedTime <= query.EndTime.Value);

            return await q.OrderByDescending(a => a.CreatedTime).ToPagedAsync(query.PageIndex, query.PageSize);
        }
    }
}