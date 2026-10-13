using JfYu.Redis.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Services;
using Jyz.UnitTests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Services
{
    public class RoleServiceTests
    {
        // ─── GetPagedAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAllRoles()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Editor" },
                new Role { Name = "Viewer" });
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var result = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyName_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Editor", Description = "edits content" });
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var result = await svc.GetPagedAsync(new QueryRequest { SearchKey = "Admin", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Name.Should().Be("Admin");
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyDescription_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Roles.AddRange(
                new Role { Name = "Admin", Description = "manages everything" },
                new Role { Name = "Editor", Description = "edits content" });
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var result = await svc.GetPagedAsync(new QueryRequest { SearchKey = "edits", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Name.Should().Be("Editor");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Roles.AddRange(
                new Role { Name = "Active", Status = 1 },
                new Role { Name = "Inactive", Status = 0 });
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var result = await svc.GetPagedAsync(new QueryRequest { Status = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Name.Should().Be("Active");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDateRange_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var r1 = new Role { Name = "Old" };
            var r2 = new Role { Name = "New" };
            ctx.Roles.AddRange(r1, r2);
            await ctx.SaveChangesAsync();
            r1.CreatedTime = now.AddDays(-10);
            r2.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var result = await svc.GetPagedAsync(new QueryRequest
            {
                StartTime = now.AddDays(-3),
                EndTime = now,
                PageIndex = 1,
                PageSize = 10
            });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Name.Should().Be("New");
        }

        [Fact]
        public async Task GetPagedAsync_Paginates_Correctly()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            for (int i = 1; i <= 12; i++)
                ctx.Roles.Add(new Role { Name = $"Role{i:00}" });
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            var page1 = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });
            var page2 = await svc.GetPagedAsync(new QueryRequest { PageIndex = 2, PageSize = 10 });

            page1.Data!.Count.Should().Be(10);
            page2.Data!.Count.Should().Be(2);
        }

        // ─── AssignPermissionsAsync ───────────────────────────────────────────

        [Fact]
        public async Task AssignPermissionsAsync_RoleNotFound_IsNoOp()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            // Should not throw — returns silently
            await svc.AssignPermissionsAsync(999, [1, 2]);

            ctx.Roles.Should().BeEmpty();
        }

        [Fact]
        public async Task AssignPermissionsAsync_AssignsPermissions()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var perm1 = new Permission { Id = 1, Code = "user:get", Name = "user:get" };
            var perm2 = new Permission { Id = 2, Code = "user:edit", Name = "user:edit" };
            var role = new Role { Id = 5, Name = "Editor" };
            ctx.Permissions.AddRange(perm1, perm2);
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            await svc.AssignPermissionsAsync(5, [1, 2]);

            var updated = await ctx.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == 5);
            updated.Permissions.Select(p => p.Id).Should().BeEquivalentTo([1, 2]);
        }

        [Fact]
        public async Task AssignPermissionsAsync_ReplacesExistingPermissions()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var perm1 = new Permission { Id = 1, Code = "a", Name = "a" };
            var perm2 = new Permission { Id = 2, Code = "b", Name = "b" };
            var perm3 = new Permission { Id = 3, Code = "c", Name = "c" };
            var role = new Role { Id = 5, Name = "Test" };
            ctx.Permissions.AddRange(perm1, perm2, perm3);
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync();
            role.Permissions.Add(perm1);
            role.Permissions.Add(perm2);
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            await svc.AssignPermissionsAsync(5, [3]);

            var updated = await ctx.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == 5);
            updated.Permissions.Should().ContainSingle(p => p.Id == 3);
        }

        [Fact]
        public async Task AssignPermissionsAsync_EmptyList_ClearsPermissions()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var perm = new Permission { Id = 1, Code = "a", Name = "a" };
            var role = new Role { Id = 5, Name = "Test" };
            ctx.Permissions.Add(perm);
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync();
            role.Permissions.Add(perm);
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            await svc.AssignPermissionsAsync(5, []);

            var updated = await ctx.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == 5);
            updated.Permissions.Should().BeEmpty();
        }

        [Fact]
        public async Task AssignPermissionsAsync_IgnoresNonExistentPermissionIds()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var perm = new Permission { Id = 1, Code = "a", Name = "a" };
            var role = new Role { Id = 5, Name = "Test" };
            ctx.Permissions.Add(perm);
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync();
            var svc = new RoleService(ctx, ro
                , new Mock<IRedisService>().Object, NullLogger<RoleService>.Instance
            );

            // 999 doesn't exist — should just assign perm 1
            await svc.AssignPermissionsAsync(5, [1, 999]);

            var updated = await ctx.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == 5);
            updated.Permissions.Should().ContainSingle(p => p.Id == 1);
        }

        [Fact]
        public async Task AssignPermissionsAsync_PersistsChange_WhenRedisInvalidationThrows()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var perm = new Permission { Id = 1, Code = "a", Name = "a" };
            var role = new Role { Id = 5, Name = "Test" };
            var user = new User { Id = 1, UserName = "u", Status = 1, Roles = [role] };
            ctx.Permissions.Add(perm);
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.RemoveAllAsync(It.IsAny<List<string>>()))
                .ThrowsAsync(new TimeoutException("redis unavailable"));
            var svc = new RoleService(ctx, ro, redisMock.Object, NullLogger<RoleService>.Instance);

            var act = () => svc.AssignPermissionsAsync(5, [1]);
            await act.Should().NotThrowAsync();

            var updated = await ctx.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == 5);
            updated.Permissions.Should().ContainSingle(p => p.Id == 1);
        }
    }
}


