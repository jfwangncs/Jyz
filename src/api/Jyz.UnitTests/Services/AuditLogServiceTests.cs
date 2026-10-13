using Jyz.Entity;
using Jyz.Model.AuditLog;
using Jyz.Services;
using Jyz.UnitTests.TestBase;

namespace Jyz.UnitTests.Services
{
    public class AuditLogServiceTests
    {
        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAll()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "User", Action = "GET" },
                new AuditLog { Resource = "Role", Action = "POST" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKey_UserName_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "User", Action = "GET", UserName = "alice" },
                new AuditLog { Resource = "Role", Action = "POST", UserName = "bob" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { SearchKey = "alice", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().UserName.Should().Be("alice");
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKey_Resource_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "UserResource", Action = "GET" },
                new AuditLog { Resource = "RoleResource", Action = "POST" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { SearchKey = "User", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByResource_ReturnsExactMatch()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "User", Action = "GET" },
                new AuditLog { Resource = "Role", Action = "POST" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { Resource = "User", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByAction_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "User", Action = "GET" },
                new AuditLog { Resource = "Role", Action = "POST" },
                new AuditLog { Resource = "Perm", Action = "GET" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { Action = "GET", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByUserId_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.AuditLogs.AddRange(
                new AuditLog { Resource = "User", Action = "GET", UserId = 1 },
                new AuditLog { Resource = "Role", Action = "POST", UserId = 2 });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { UserId = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDateRange_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var old = new AuditLog { Resource = "Old", Action = "GET" };
            var recent = new AuditLog { Resource = "Recent", Action = "GET" };
            ctx.AuditLogs.AddRange(old, recent);
            await ctx.SaveChangesAsync();
            old.CreatedTime = now.AddDays(-10);
            recent.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest
            {
                StartTime = now.AddDays(-3),
                EndTime = now,
                PageIndex = 1,
                PageSize = 10
            });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Resource.Should().Be("Recent");
        }

        [Fact]
        public async Task GetPagedAsync_OrderedByCreatedTimeDesc()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var a1 = new AuditLog { Resource = "First", Action = "GET" };
            var a2 = new AuditLog { Resource = "Second", Action = "POST" };
            ctx.AuditLogs.AddRange(a1, a2);
            await ctx.SaveChangesAsync();
            a1.CreatedTime = now.AddDays(-10);
            a2.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { PageIndex = 1, PageSize = 10 });

            result.Data!.First().Resource.Should().Be("Second");
        }

        [Fact]
        public async Task GetPagedAsync_Paginates_Correctly()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            for (int i = 1; i <= 7; i++)
                ctx.AuditLogs.Add(new AuditLog { Resource = $"R{i}", Action = "GET" });
            await ctx.SaveChangesAsync();
            var svc = new AuditLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryAuditLogRequest { PageIndex = 1, PageSize = 5 });

            result.Data!.Count.Should().Be(5);
            result.TotalCount.Should().Be(7);
        }
    }
}
