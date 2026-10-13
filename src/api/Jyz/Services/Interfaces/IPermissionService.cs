using JfYu.Data.Service;
using Jyz.Entity;

namespace Jyz.Services.Interfaces
{
    public interface IPermissionService : IService<Permission, AppDbContext>
    {
        void SyncAsync();

        void GrantAllPermissionsToDefaultAdminRole();
    }
}
