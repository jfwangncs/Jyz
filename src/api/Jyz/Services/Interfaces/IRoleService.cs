using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model;

namespace Jyz.Services.Interfaces
{
    public interface IRoleService : IService<Role, AppDbContext>
    {
        Task<PagedData<Role>> GetPagedAsync(QueryRequest query);

        Task AssignPermissionsAsync(int roleId, List<int> permissionIds);
    }
}
