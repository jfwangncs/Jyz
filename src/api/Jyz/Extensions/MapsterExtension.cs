using Mapster;
using JfYu.Data.Model;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.Request;
using Jyz.Model.Response;
using Jyz.Model.Role;

namespace Jyz.Extensions
{
    public static class MapsterExtension
    {
        public static IServiceCollection AddMapster(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);
            TypeAdapterConfig.GlobalSettings.ForType(typeof(PagedData<>), typeof(PagedResult<>))
                .Map("Items", "Data")
                .Map("Total", "TotalCount");
            TypeAdapterConfig<UpdateRoleRequest, Role>.NewConfig().IgnoreNullValues(true);
            TypeAdapterConfig<UpdateUserRequest, User>.NewConfig().IgnoreNullValues(true);
            TypeAdapterConfig<User, UserResponse>.ForType().Map(q => q.Roles, q => q.Roles.Where(r => r.Status == (int)DataStatus.Active).SelectMany(r => r.Permissions).Where(p => p.Status == (int)DataStatus.Active).Select(p => p.Code).Distinct().ToList())
            .Map(q => q.RoleList, q => q.Roles.Select(r => r.Adapt<RoleResponse>()).ToList());
            return services;
        }
    }
}
