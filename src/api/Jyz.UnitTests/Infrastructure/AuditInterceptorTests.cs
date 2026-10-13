using System.Net;
using System.Security.Claims;
using Jyz.Entity;
using Jyz.Infrastructure;
using Jyz.UnitTests.TestBase;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Infrastructure
{
    public class AuditInterceptorTests
    {
        private static (AppDbContext ctx, AuditInterceptor interceptor) CreateContext(
            int? userId = 42,
            string? userName = "alice",
            string? ip = "10.0.0.1")
        {
            var http = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                };
                if (userName != null)
                    claims.Add(new Claim(ClaimTypes.Name, userName));
                http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            }
            if (!string.IsNullOrEmpty(ip))
                http.Connection.RemoteIpAddress = IPAddress.Parse(ip);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var interceptor = new AuditInterceptor(accessor);

            var (ctx, _) = DbContextFactory.CreateInMemory(interceptors: [interceptor]);
            return (ctx, interceptor);
        }

        [Fact]
        public async Task WritesAuditLog_OnAdd()
        {
            var (ctx, _) = CreateContext();

            ctx.DictTypes.Add(new DictType { Code = "c1", Name = "n1", Status = 1 });
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs.AsNoTracking().ToListAsync();
            logs.Should().HaveCount(1);
            var log = logs[0];
            log.Action.Should().Be("Added");
            log.Resource.Should().Be(nameof(DictType));
            log.UserId.Should().Be(42);
            log.UserName.Should().Be("alice");
            log.IP.Should().Be("10.0.0.1");
            log.ResourceId.Should().NotBeNullOrEmpty();
            log.OldValue.Should().BeNull();
            log.NewValue.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WritesAuditLog_OnModify_WithOldAndNewValues()
        {
            var (ctx, _) = CreateContext();
            var dt = new DictType { Code = "c1", Name = "n1", Status = 1 };
            ctx.DictTypes.Add(dt);
            await ctx.SaveChangesAsync();

            dt.Name = "n2";
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs.AsNoTracking().OrderBy(a => a.Id).ToListAsync();
            logs.Should().HaveCount(2);
            var modifyLog = logs[1];
            modifyLog.Action.Should().Be("Modified");
            modifyLog.OldValue.Should().Contain("n1");
            modifyLog.NewValue.Should().Contain("n2");
            modifyLog.ResourceId.Should().Be(dt.Id.ToString());
        }

        [Fact]
        public async Task WritesAuditLog_OnDelete_WithoutNewValue()
        {
            var (ctx, _) = CreateContext();
            var dt = new DictType { Code = "c1", Name = "n1", Status = 1 };
            ctx.DictTypes.Add(dt);
            await ctx.SaveChangesAsync();

            ctx.DictTypes.Remove(dt);
            await ctx.SaveChangesAsync();

            var deleteLog = await ctx.AuditLogs.AsNoTracking().OrderByDescending(a => a.Id).FirstAsync();
            deleteLog.Action.Should().Be("Deleted");
            deleteLog.OldValue.Should().NotBeNullOrEmpty();
            deleteLog.NewValue.Should().BeNull();
        }

        [Fact]
        public async Task SkipsAuditLogEntities_ToAvoidRecursion()
        {
            var (ctx, _) = CreateContext();

            // Two writes from a single SaveChangesAsync would loop if AuditLog itself was audited.
            ctx.DictTypes.Add(new DictType { Code = "c1", Name = "n1", Status = 1 });
            await ctx.SaveChangesAsync();

            var logs = await ctx.AuditLogs.AsNoTracking().ToListAsync();
            logs.Should().HaveCount(1);
        }

        [Fact]
        public async Task DoesNothing_WhenNoTrackedEntries()
        {
            var (ctx, _) = CreateContext();

            await ctx.SaveChangesAsync();

            (await ctx.AuditLogs.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task CapturesNullUser_WhenAnonymous()
        {
            var (ctx, _) = CreateContext(userId: null, userName: null, ip: null);

            ctx.DictTypes.Add(new DictType { Code = "c1", Name = "n1", Status = 1 });
            await ctx.SaveChangesAsync();

            var log = await ctx.AuditLogs.AsNoTracking().FirstAsync();
            log.UserId.Should().BeNull();
            log.UserName.Should().BeNull();
            log.IP.Should().BeNull();
        }
    }
}
