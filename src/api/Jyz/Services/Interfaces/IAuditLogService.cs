using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.AuditLog;

namespace Jyz.Services.Interfaces
{
    public interface IAuditLogService : IService<AuditLog, AppDbContext>
    {
        Task<PagedData<AuditLog>> GetPagedAsync(QueryAuditLogRequest query);
    }
}
