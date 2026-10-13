using Jyz.Constants;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.Permission;
using Jyz.Services;
using Jyz.Services.Interfaces;
using Jyz.UnitTests.TestBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jyz.UnitTests.Services
{
    public class PermissionServiceTests
    {
        /// <summary>
        /// Throws <see cref="DbUpdateException"/> from <c>SaveChanges</c> whenever the pending
        /// change set includes an Added <see cref="Permission"/> whose Code is in <paramref name="codesToFail"/>,
        /// to deterministically exercise PermissionService.SyncAsync()'s catch(DbUpdateException) branches.
        /// </summary>
        private sealed class ThrowOnPermissionCodeInterceptor(IEnumerable<string> codesToFail) : SaveChangesInterceptor
        {
            private readonly HashSet<string> _codesToFail = [.. codesToFail];

            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                if (eventData.Context is Entity.AppDbContext context)
                {
                    var hasFailingAdd = context.ChangeTracker.Entries<Permission>()
                        .Any(e => e.State == EntityState.Added && _codesToFail.Contains(e.Entity.Code));
                    if (hasFailingAdd)
                        throw new DbUpdateException("Simulated duplicate key violation");
                }
                return base.SavingChanges(eventData, result);
            }
        }

        private static PermissionService CreateService(out (Entity.AppDbContext ctx, JfYu.Data.Context.ReadonlyDBContext<Entity.AppDbContext> ro) bag)
        {
            bag = DbContextFactory.CreateInMemory();
            return new PermissionService(bag.ctx, bag.ro, NullLogger<PermissionService>.Instance);
        }

        private static PermissionService CreateServiceWithInterceptor(
            IEnumerable<string> codesToFail,
            out (Entity.AppDbContext ctx, JfYu.Data.Context.ReadonlyDBContext<Entity.AppDbContext> ro) bag)
        {
            bag = DbContextFactory.CreateInMemory(interceptors: [new ThrowOnPermissionCodeInterceptor(codesToFail)]);
            return new PermissionService(bag.ctx, bag.ro, NullLogger<PermissionService>.Instance);
        }

        // ─── GetPagedAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAllPermissions()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "a", Name = "A" },
                new Permission { Code = "b", Name = "B" },
                new Permission { Code = "c", Name = "C" });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });

            result.Total.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyCode_ReturnsMatches()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "user:get", Name = "Get User" },
                new Permission { Code = "role:edit", Name = "Edit Role" });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { SearchKey = "user", PageIndex = 1, PageSize = 10 });

            result.Total.Should().Be(1);
            result.Items.First().Code.Should().Be("user:get");
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyName_ReturnsMatches()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "a", Name = "Alpha" },
                new Permission { Code = "b", Name = "Beta" });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { SearchKey = "Beta", PageIndex = 1, PageSize = 10 });

            result.Total.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ReturnsMatches()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "active", Name = "Active", Status = 1 },
                new Permission { Code = "inactive", Name = "Inactive", Status = 0 });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { Status = 1, PageIndex = 1, PageSize = 10 });

            result.Total.Should().Be(1);
            result.Items.First().Code.Should().Be("active");
        }

        [Fact]
        public async Task GetPagedAsync_OrderedBySortThenId()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "z", Name = "Z", Sort = 10 },
                new Permission { Code = "a", Name = "A", Sort = 1 },
                new Permission { Code = "m", Name = "M", Sort = 5 });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });

            result.Items[0].Code.Should().Be("a");
            result.Items[1].Code.Should().Be("m");
            result.Items[2].Code.Should().Be("z");
        }

        [Fact]
        public async Task GetPagedAsync_Paginates_Correctly()
        {
            var svc = CreateService(out var bag);
            for (int i = 1; i <= 8; i++)
                bag.ctx.Permissions.Add(new Permission { Code = $"p{i}", Name = $"P{i}" });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 5 });

            result.Items.Count.Should().Be(5);
            result.Total.Should().Be(8);
        }

        [Fact]
        public async Task GetPagedAsync_FiltersByStartAndEndTime()
        {
            var svc = CreateService(out var bag);
            var now = System.DateTime.UtcNow;
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "old", Name = "old", CreatedTime = now.AddDays(-10) },
                new Permission { Code = "mid", Name = "mid", CreatedTime = now.AddDays(-2) },
                new Permission { Code = "new", Name = "new", CreatedTime = now });
            await bag.ctx.SaveChangesAsync();

            var result = await svc.GetPagedAsync(new QueryRequest
            {
                StartTime = now.AddDays(-5),
                EndTime = now.AddDays(-1),
                PageIndex = 1,
                PageSize = 10,
            });

            result.Total.Should().Be(1);
            result.Items[0].Code.Should().Be("mid");
        }

        // ─── SyncAsync ────────────────────────────────────────────────────────

        [Fact]
        public void SyncAsync_SeedsPermissionsFromControllerAttributes()
        {
            var svc = CreateService(out var bag);

            svc.SyncAsync();

            // Parent menu ("system") shared by RBAC controllers should be seeded.
            bag.ctx.Permissions.Should().Contain(p => p.Code == Jyz.Constants.PermissionCodes.System);
            bag.ctx.Permissions.Count().Should().BeGreaterThan(1);
        }

        [Fact]
        public void SyncAsync_IsIdempotent()
        {
            var svc = CreateService(out var bag);

            svc.SyncAsync();
            var firstCount = bag.ctx.Permissions.Count();
            svc.SyncAsync();
            var secondCount = bag.ctx.Permissions.Count();

            secondCount.Should().Be(firstCount);
        }

        [Fact]
        public void SyncAsync_ParentPermissionInsertThrowsDbUpdateException_DoesNotThrowAndMarksCodeExisting()
        {
            var svc = CreateServiceWithInterceptor([PermissionCodes.System], out var bag);

            var act = svc.SyncAsync;

            act.Should().NotThrow();
            // The parent ("system") insert failed (simulated duplicate-key race) and was swallowed,
            // so no "system" row exists, but sync still proceeded to seed the scanned child permissions.
            bag.ctx.Permissions.Should().NotContain(p => p.Code == PermissionCodes.System);
            bag.ctx.Permissions.Should().Contain(p => p.Code == PermissionCodes.RoleGet);
        }

        [Fact]
        public void SyncAsync_ChildPermissionInsertThrowsDbUpdateException_DoesNotThrowAndSkipsThatCode()
        {
            var svc = CreateServiceWithInterceptor([PermissionCodes.RoleGet], out var bag);

            var act = svc.SyncAsync;

            act.Should().NotThrow();
            bag.ctx.Permissions.Should().Contain(p => p.Code == PermissionCodes.System);
            bag.ctx.Permissions.Should().NotContain(p => p.Code == PermissionCodes.RoleGet);
            // Other scanned permissions were unaffected by the one simulated failure.
            bag.ctx.Permissions.Should().Contain(p => p.Code == PermissionCodes.RoleAdd);
        }

        // ─── GrantAllPermissionsToDefaultAdminRole ─────────────────────────────

        [Fact]
        public void GrantAllPermissionsToDefaultAdminRole_RoleHasNoPermissions_GrantsAll()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Roles.Add(new Role { Id = 1, Name = "Admin" });
            bag.ctx.Permissions.AddRange(
                new Permission { Code = "a", Name = "A" },
                new Permission { Code = "b", Name = "B" });
            bag.ctx.SaveChanges();

            svc.GrantAllPermissionsToDefaultAdminRole();

            var role = bag.ctx.Roles.Include(r => r.Permissions).First(r => r.Id == 1);
            role.Permissions.Should().HaveCount(2);
        }

        [Fact]
        public void GrantAllPermissionsToDefaultAdminRole_RoleAlreadyHasPermissions_DoesNotOverwrite()
        {
            var svc = CreateService(out var bag);
            var kept = new Permission { Code = "kept", Name = "Kept" };
            bag.ctx.Permissions.AddRange(kept, new Permission { Code = "other", Name = "Other" });
            bag.ctx.Roles.Add(new Role { Id = 1, Name = "Admin", Permissions = [kept] });
            bag.ctx.SaveChanges();

            svc.GrantAllPermissionsToDefaultAdminRole();

            var role = bag.ctx.Roles.Include(r => r.Permissions).First(r => r.Id == 1);
            role.Permissions.Should().ContainSingle(p => p.Code == "kept");
        }

        [Fact]
        public void GrantAllPermissionsToDefaultAdminRole_NoRoleWithId1_DoesNotThrow()
        {
            var svc = CreateService(out var bag);
            bag.ctx.Permissions.Add(new Permission { Code = "a", Name = "A" });
            bag.ctx.SaveChanges();

            var act = () => svc.GrantAllPermissionsToDefaultAdminRole();

            act.Should().NotThrow();
        }
    }
}
