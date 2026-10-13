using Jyz.Constants;
using Jyz.Entity;
using Jyz.Model.LoginLog;
using Jyz.Services;
using Jyz.UnitTests.TestBase;

namespace Jyz.UnitTests.Services
{
    public class LoginLogServiceTests
    {
        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAll()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.LoginLogs.AddRange(
                new LoginLog { UserName = "alice", IP = "1.1.1.1", Result = 0, Platform = (int)Platform.Web },
                new LoginLog { UserName = "bob", IP = "2.2.2.2", Result = 1, Platform = (int)Platform.Web });
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKey_UserName_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.LoginLogs.AddRange(
                new LoginLog { UserName = "alice", IP = "1.1.1.1", Result = 0 },
                new LoginLog { UserName = "bob", IP = "2.2.2.2", Result = 0 });
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { SearchKey = "alice", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().UserName.Should().Be("alice");
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKey_IP_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.LoginLogs.AddRange(
                new LoginLog { UserName = "alice", IP = "192.168.1.1", Result = 0 },
                new LoginLog { UserName = "bob", IP = "10.0.0.1", Result = 0 });
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { SearchKey = "192", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByResult_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.LoginLogs.AddRange(
                new LoginLog { UserName = "a", IP = "1.1.1.1", Result = 0 },  // success
                new LoginLog { UserName = "b", IP = "2.2.2.2", Result = 1 },  // fail
                new LoginLog { UserName = "c", IP = "3.3.3.3", Result = 1 }); // fail
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { Result = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByPlatform_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.LoginLogs.AddRange(
                new LoginLog { UserName = "a", IP = "1.1.1.1", Result = 0, Platform = (int)Platform.Web },
                new LoginLog { UserName = "b", IP = "2.2.2.2", Result = 0, Platform = (int)Platform.Wechat });
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest
            {
                Platform = (int)Platform.Web,
                PageIndex = 1,
                PageSize = 10
            });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDateRange_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var old = new LoginLog { UserName = "old", IP = "1.1.1.1", Result = 0 };
            var recent = new LoginLog { UserName = "recent", IP = "2.2.2.2", Result = 0 };
            ctx.LoginLogs.AddRange(old, recent);
            await ctx.SaveChangesAsync();
            old.CreatedTime = now.AddDays(-10);
            recent.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest
            {
                StartTime = now.AddDays(-3),
                EndTime = now,
                PageIndex = 1,
                PageSize = 10
            });

            result.TotalCount.Should().Be(1);
            result.Data!.First().UserName.Should().Be("recent");
        }

        [Fact]
        public async Task GetPagedAsync_OrderedByCreatedTimeDesc()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var l1 = new LoginLog { UserName = "first", IP = "1.1.1.1", Result = 0 };
            var l2 = new LoginLog { UserName = "second", IP = "2.2.2.2", Result = 0 };
            ctx.LoginLogs.AddRange(l1, l2);
            await ctx.SaveChangesAsync();
            l1.CreatedTime = now.AddDays(-5);
            l2.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { PageIndex = 1, PageSize = 10 });

            result.Data!.First().UserName.Should().Be("second");
        }

        [Fact]
        public async Task GetPagedAsync_Paginates_Correctly()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            for (int i = 1; i <= 9; i++)
                ctx.LoginLogs.Add(new LoginLog { UserName = $"u{i}", IP = "1.1.1.1", Result = 0 });
            await ctx.SaveChangesAsync();
            var svc = new LoginLogService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryLoginLogRequest { PageIndex = 1, PageSize = 5 });

            result.Data!.Count.Should().Be(5);
            result.TotalCount.Should().Be(9);
        }
    }
}
