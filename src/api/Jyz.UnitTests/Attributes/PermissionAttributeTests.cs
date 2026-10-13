using System.Security.Claims;
using JfYu.Data.Context;
using JfYu.Redis.Interface;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Model;
using Jyz.UnitTests.TestBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Jyz.UnitTests.Attributes
{
    public class PermissionAttributeTests
    {
        private static AuthorizationFilterContext CreateContext(
            ClaimsPrincipal user,
            IServiceProvider? services = null,
            bool allowAnonymous = false)
        {
            var http = new DefaultHttpContext { User = user };
            if (services != null)
                http.RequestServices = services;

            var actionDescriptor = new ActionDescriptor
            {
                EndpointMetadata = allowAnonymous ? [new AllowAnonymousAttribute()] : []
            };
            var actionContext = new ActionContext(http, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, []);
        }

        private static ClaimsPrincipal CreatePrincipal(int userId, params string[] permissions)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, "user")
            };
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public async Task SkipsCheck_WhenAllowAnonymous()
        {
            var attr = new PermissionAttribute("user:add");
            var ctx = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()), allowAnonymous: true);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
        }

        [Fact]
        public async Task Returns401_WhenUserNotAuthenticated()
        {
            var attr = new PermissionAttribute("user:add");
            var ctx = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

            await attr.OnAuthorizationAsync(ctx);

            var obj = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task SkipsCheck_WhenTypeIsMenu()
        {
            var attr = new PermissionAttribute("dashboard", PermissionType.Menu);
            var principal = CreatePrincipal(1);
            var ctx = CreateContext(principal);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
        }

        [Fact]
        public async Task ReadsPermissionsFromClaims_WhenNoRedis()
        {
            var attr = new PermissionAttribute("user:add");
            var principal = CreatePrincipal(1, "user:add", "user:edit");

            var services = new ServiceCollection().BuildServiceProvider();
            var ctx = CreateContext(principal, services);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
        }

        [Fact]
        public async Task Returns403_WhenPermissionMissing()
        {
            var attr = new PermissionAttribute("user:delete");
            var principal = CreatePrincipal(1, "user:add");

            var services = new ServiceCollection().BuildServiceProvider();
            var ctx = CreateContext(principal, services);

            await attr.OnAuthorizationAsync(ctx);

            var obj = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UsesCachedPermissions_FromRedis()
        {
            var attr = new PermissionAttribute("user:add");
            var principal = CreatePrincipal(1);

            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.GetAsync<List<string>>(It.IsAny<string>()))
                .ReturnsAsync(["user:add"]);

            var services = new ServiceCollection()
                .AddSingleton(redisMock.Object)
                .BuildServiceProvider();
            var ctx = CreateContext(principal, services);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
            redisMock.Verify(r => r.GetAsync<List<string>>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LoadsPermissionsFromDb_AndCachesThem_WhenCacheMiss()
        {
            var (db, _) = DbContextFactory.CreateInMemory();
            var perm = new Permission { Id = 1, Code = "user:add", Type = PermissionType.Button, Status = 1, Name = "AddUser" };
            var role = new Role { Id = 1, Name = "admin", Status = 1, Permissions = [perm] };
            var user = new User { Id = 1, UserName = "u", Status = 1, Roles = [role] };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.GetAsync<List<string>>(It.IsAny<string>()))
                .ReturnsAsync((List<string>?)null);

            var services = new ServiceCollection()
                .AddSingleton(redisMock.Object)
                .AddSingleton(db)
                .BuildServiceProvider();

            var attr = new PermissionAttribute("user:add");
            var principal = CreatePrincipal(1);
            var ctx = CreateContext(principal, services);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
            redisMock.Verify(
                r => r.AddAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task FallsBackToDb_WhenRedisGetThrows()
        {
            var (db, _) = DbContextFactory.CreateInMemory();
            var perm = new Permission { Id = 1, Code = "user:add", Type = PermissionType.Button, Status = 1, Name = "AddUser" };
            var role = new Role { Id = 1, Name = "admin", Status = 1, Permissions = [perm] };
            var user = new User { Id = 1, UserName = "u", Status = 1, Roles = [role] };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.GetAsync<List<string>>(It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("redis unavailable"));
            redisMock.Setup(r => r.AddAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new TimeoutException("redis unavailable"));

            var services = new ServiceCollection()
                .AddSingleton(redisMock.Object)
                .AddSingleton(db)
                .BuildServiceProvider();

            var attr = new PermissionAttribute("user:add");
            var principal = CreatePrincipal(1);
            var ctx = CreateContext(principal, services);

            await attr.OnAuthorizationAsync(ctx);

            ctx.Result.Should().BeNull();
        }
    }
}
