using Jyz.Extensions;
using Jyz.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jyz.UnitTests.Extensions
{
    public class InjectionExtensionTests
    {
        private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:EndPoints:0:Host"] = "localhost",
                ["Redis:EndPoints:0:Port"] = "6379",
                ["Redis:Timeout"] = "200",
                ["Redis:AsyncTimeout"] = "200",
                ["Redis:SyncTimeout"] = "200",
                ["MiniProgram:AppId"] = "test-app-id",
                ["MiniProgram:AppSecret"] = "test-app-secret",
                ["ConnectionStrings:DatabaseType"] = "PostgreSQL",
                ["ConnectionStrings:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test",
            })
            .Build();

        [Fact]
        public void AddCustomInjection_RegistersAllApplicationServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();

            services.AddCustomInjection(BuildConfiguration());
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IJwtService>().Should().BeOfType<Jyz.Services.JwtService>();
            scope.ServiceProvider.GetRequiredService<IRoleService>().Should().BeOfType<Jyz.Services.RoleService>();
            scope.ServiceProvider.GetRequiredService<IPermissionService>().Should().BeOfType<Jyz.Services.PermissionService>();
            scope.ServiceProvider.GetRequiredService<IUserService>().Should().BeOfType<Jyz.Services.UserService>();
            scope.ServiceProvider.GetRequiredService<IAuditLogService>().Should().BeOfType<Jyz.Services.AuditLogService>();
            scope.ServiceProvider.GetRequiredService<ILoginLogService>().Should().BeOfType<Jyz.Services.LoginLogService>();
            scope.ServiceProvider.GetRequiredService<IDictTypeService>().Should().BeOfType<Jyz.Services.DictTypeService>();
            scope.ServiceProvider.GetRequiredService<IDictItemService>().Should().BeOfType<Jyz.Services.DictItemService>();
            scope.ServiceProvider.GetRequiredService<IRedisCacheService>().Should().BeOfType<Jyz.Services.RedisCacheService>();
        }
    }
}
