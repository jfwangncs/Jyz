using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model.LoginLog;

namespace Jyz.Services.Interfaces
{
    public interface ILoginLogService : IService<LoginLog, AppDbContext>
    {
        Task<PagedData<LoginLog>> GetPagedAsync(QueryLoginLogRequest query);
    }
}