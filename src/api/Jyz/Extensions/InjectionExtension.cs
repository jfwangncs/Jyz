using JfYu.Data.Extension;
using Jyz.Infrastructure;
using JfYu.Redis.Extensions;
using JfYu.WeChat;
using Jyz.Services;
using Jyz.Services.Interfaces;
using Jyz.Entity;
using Microsoft.EntityFrameworkCore.Diagnostics;
using JfYu.Data.PostgreSQL;

namespace Jyz.Extensions
{
    public static class InjectionExtension
    {
        public static IServiceCollection AddCustomInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IJwtService, JwtService>();

            services.AddRedisService(options => { configuration.GetSection("Redis").Bind(options); });
            services.AddScoped<IRedisCacheService, RedisCacheService>();

            services.AddMiniProgram(options => { configuration.GetSection("MiniProgram").Bind(options); });

            services.AddScoped<IInterceptor, AuditInterceptor>();
            services.AddJfYuPostgreSql();
            services.AddJfYuDbContext<AppDbContext>(options =>
            {
                configuration.GetSection("ConnectionStrings").Bind(options);
            });
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<ILoginLogService, LoginLogService>();
            services.AddScoped<IDictTypeService, DictTypeService>();
            services.AddScoped<IDictItemService, DictItemService>();
            return services;
        }
    }
}
