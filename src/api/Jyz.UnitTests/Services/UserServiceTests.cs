using JfYu.Data.Model;
using JfYu.Redis.Interface;
using IRedisDatabase = StackExchange.Redis.IDatabase;
using RedisValue = StackExchange.Redis.RedisValue;
using Microsoft.Extensions.Logging;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Exceptions;
using Jyz.Model;
using Jyz.Model.Request;
using Jyz.Model.User;
using Jyz.Services;
using Jyz.UnitTests.TestBase;
using JfYu.WeChat;
using JfYu.WeChat.Model.Response;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Services
{
    public class UserServiceTests
    {
        private static UserService CreateSvc(AppDbContext ctx, JfYu.Data.Context.ReadonlyDBContext<AppDbContext> ro
            , IMiniProgram? miniProgram = null
            )
        {
            var redisMock = new Mock<IRedisService>();
            var dbMock = new Mock<IRedisDatabase>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            return new UserService(ctx, ro
                , miniProgram ?? new Mock<IMiniProgram>().Object
                , redisMock.Object
                , Microsoft.Extensions.Options.Options.Create(new Jyz.Options.JwtSettings { Expires = 60 })
                , new Mock<ILogger<UserService>>().Object
            );
        }

        private static UserService CreateService()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            return CreateSvc(ctx, ro);
        }

        private static UserService CreateService(out dynamic ctxBag)
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctxBag = ctx;
            return CreateSvc(ctx, ro);
        }

        // ─── LoginAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUser()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro);
            var password = BCrypt.Net.BCrypt.HashPassword("pass123");
            ctx.Users.Add(new User { UserName = "alice", Password = password });
            await ctx.SaveChangesAsync();

            var result = await svc.LoginAsync(new LoginRequest
            {
                UserName = "alice",
                Password = "pass123",
                Platform = Platform.Web
            });

            result.Should().NotBeNull();
            result!.UserName.Should().Be("alice");
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsBusinessException()
        {
            var svc = CreateService();

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                UserName = "nobody",
                Password = "pass",
                Platform = Platform.Web
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsBusinessException()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro);
            ctx.Users.Add(new User { UserName = "bob", Password = BCrypt.Net.BCrypt.HashPassword("correct") });
            await ctx.SaveChangesAsync();

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                UserName = "bob",
                Password = "wrong",
                Platform = Platform.Web
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.InvalidCredentials);
        }

        [Fact]
        public async Task LoginAsync_PasswordNotSet_ThrowsBusinessException()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro);
            ctx.Users.Add(new User { UserName = "carol", Password = null });
            await ctx.SaveChangesAsync();

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                UserName = "carol",
                Password = "any",
                Platform = Platform.Web
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.PasswordNotSet);
        }

        [Fact]
        public async Task LoginAsync_UpdatesLastLoginTime()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro);
            var before = DateTime.UtcNow.AddMinutes(-1);
            ctx.Users.Add(new User { UserName = "dave", Password = BCrypt.Net.BCrypt.HashPassword("p") });
            await ctx.SaveChangesAsync();

            await svc.LoginAsync(new LoginRequest { UserName = "dave", Password = "p", Platform = Platform.Web });

            var updated = await ctx.Users.FirstAsync(u => u.UserName == "dave");
            updated.LastLoginTime.Should().BeAfter(before);
        }

        [Fact]
        public async Task LoginAsync_UnsupportedPlatform_ThrowsBusinessException()
        {
            var svc = CreateService();

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                Platform = (Platform)99
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UnsupportedLoginMethod);
        }

        [Fact]
        public async Task LoginAsync_WechatPlatform_EmptyCode_ThrowsUserNotFound()
        {
            var miniProgramMock = new Mock<IMiniProgram>();
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro, miniProgramMock.Object);

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                Platform = Platform.Wechat,
                Code = ""
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
            miniProgramMock.Verify(m => m.LoginAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WechatPlatform_MiniProgramReturnsError_ThrowsUserNotFound()
        {
            var miniProgramMock = new Mock<IMiniProgram>();
            miniProgramMock.Setup(m => m.LoginAsync("code123"))
                .ReturnsAsync(new WechatLoginResponse { ErrorMessage = "invalid code" });
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro, miniProgramMock.Object);

            var act = async () => await svc.LoginAsync(new LoginRequest
            {
                Platform = Platform.Wechat,
                Code = "code123"
            });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task LoginAsync_WechatPlatform_Success_ReturnsNewUser()
        {
            var miniProgramMock = new Mock<IMiniProgram>();
            miniProgramMock.Setup(m => m.LoginAsync("code123"))
                .ReturnsAsync(new WechatLoginResponse { OpenId = "openid-1", SessionKey = "sk-1" });
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = CreateSvc(ctx, ro, miniProgramMock.Object);

            var result = await svc.LoginAsync(new LoginRequest
            {
                Platform = Platform.Wechat,
                Code = "code123",
                Phone = "1234567890"
            });

            result.Should().NotBeNull();
            result!.UserName.Should().Be("");
            result.OpenId.Should().Be("openid-1");
            result.SessionKey.Should().Be("sk-1");
            result.Phone.Should().Be("1234567890");
        }

        // ─── GetPagedAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAllUsers()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.AddRange(
                new User { UserName = "u1" },
                new User { UserName = "u2" },
                new User { UserName = "u3" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKey_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.AddRange(
                new User { UserName = "alice" },
                new User { UserName = "bob", NickName = "bobby" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryRequest { SearchKey = "bob", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.AddRange(
                new User { UserName = "active", Status = 1 },
                new User { UserName = "inactive", Status = 0 });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryRequest { Status = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().UserName.Should().Be("active");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDateRange_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var user1 = new User { UserName = "old" };
            var user2 = new User { UserName = "new" };
            ctx.Users.AddRange(user1, user2);
            await ctx.SaveChangesAsync();
            user1.CreatedTime = now.AddDays(-10);
            user2.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryRequest
            {
                StartTime = now.AddDays(-3),
                EndTime = now,
                PageIndex = 1,
                PageSize = 10
            });

            result.TotalCount.Should().Be(1);
            result.Data!.First().UserName.Should().Be("new");
        }

        [Fact]
        public async Task GetPagedAsync_Paginates_Correctly()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            for (int i = 1; i <= 15; i++)
                ctx.Users.Add(new User { UserName = $"user{i:00}" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var page1 = await svc.GetPagedAsync(new QueryRequest { PageIndex = 1, PageSize = 10 });
            var page2 = await svc.GetPagedAsync(new QueryRequest { PageIndex = 2, PageSize = 10 });

            page1.Data!.Count.Should().Be(10);
            page2.Data!.Count.Should().Be(5);
            page1.TotalCount.Should().Be(15);
        }

        // ─── UpdateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_UserNotFound_ThrowsBusinessException()
        {
            var svc = CreateService();

            var act = async () => await svc.UpdateAsync(999, new UpdateUserRequest());

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_UpdatesFields()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", NickName = "old" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            await svc.UpdateAsync(1, new UpdateUserRequest { NickName = "new" });

            var updated = await ctx.Users.FirstAsync(u => u.Id == 1);
            updated.NickName.Should().Be("new");
        }

        [Fact]
        public async Task UpdateAsync_WithRoleIds_AssignsRoles()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var role = new Role { Id = 10, Name = "Admin" };
            ctx.Roles.Add(role);
            ctx.Users.Add(new User { Id = 1, UserName = "u" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            await svc.UpdateAsync(1, new UpdateUserRequest { RoleIds = [10] });

            var updated = await ctx.Users.Include(u => u.Roles).FirstAsync(u => u.Id == 1);
            updated.Roles.Should().ContainSingle(r => r.Id == 10);
        }

        [Fact]
        public async Task UpdateAsync_EmptyRoleIds_ClearsRoles()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var role = new Role { Id = 10, Name = "Admin" };
            var user = new User { Id = 1, UserName = "u" };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            user.Roles.Add(role);
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            await svc.UpdateAsync(1, new UpdateUserRequest { RoleIds = [] });

            var updated = await ctx.Users.Include(u => u.Roles).FirstAsync(u => u.Id == 1);
            updated.Roles.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateAsync_PersistsChange_WhenRedisPermissionCacheRemovalThrows()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var role = new Role { Id = 10, Name = "Admin" };
            ctx.Roles.Add(role);
            ctx.Users.Add(new User { Id = 1, UserName = "u" });
            await ctx.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.RemoveAsync(It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("redis unavailable"));
            var svc = new UserService(ctx, ro
                , new Mock<IMiniProgram>().Object
                , redisMock.Object
                , Microsoft.Extensions.Options.Options.Create(new Jyz.Options.JwtSettings { Expires = 60 })
                , new Mock<ILogger<UserService>>().Object
            );

            var act = () => svc.UpdateAsync(1, new UpdateUserRequest { RoleIds = [10] });
            await act.Should().NotThrowAsync();

            var updated = await ctx.Users.Include(u => u.Roles).FirstAsync(u => u.Id == 1);
            updated.Roles.Should().ContainSingle(r => r.Id == 10);
        }

        [Fact]
        public async Task UpdateAsync_StatusSetToDisabled_SetsBlacklistKey()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", Status = (int)DataStatus.Active });
            await ctx.SaveChangesAsync();

            var dbMock = new Mock<IRedisDatabase>();
            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            var svc = new UserService(ctx, ro
                , new Mock<IMiniProgram>().Object
                , redisMock.Object
                , Microsoft.Extensions.Options.Options.Create(new Jyz.Options.JwtSettings { Expires = 60 })
                , new Mock<ILogger<UserService>>().Object
            );

            await svc.UpdateAsync(1, new UpdateUserRequest { Status = (int)DataStatus.Disable });

            dbMock.Verify(d => d.StringSetAsync(
                string.Format(RedisKey.UserBlacklist, 1),
                "1",
                It.IsAny<StackExchange.Redis.Expiration>(),
                It.IsAny<StackExchange.Redis.ValueCondition>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_StatusSetToActive_RemovesBlacklistKey()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", Status = 0 });
            await ctx.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            var dbMock = new Mock<IRedisDatabase>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            var svc = new UserService(ctx, ro
                , new Mock<IMiniProgram>().Object
                , redisMock.Object
                , Microsoft.Extensions.Options.Options.Create(new Jyz.Options.JwtSettings { Expires = 60 })
                , new Mock<ILogger<UserService>>().Object
            );

            await svc.UpdateAsync(1, new UpdateUserRequest { Status = (int)DataStatus.Active });

            redisMock.Verify(r => r.RemoveAsync(string.Format(RedisKey.UserBlacklist, 1)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_StatusSetToDisabled_WhenRedisThrows_DoesNotThrow()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", Status = (int)DataStatus.Active });
            await ctx.SaveChangesAsync();

            var redisMock = new Mock<IRedisService>();
            var dbMock = new Mock<IRedisDatabase>();
            dbMock.Setup(d => d.StringSetAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<StackExchange.Redis.Expiration>(),
                It.IsAny<StackExchange.Redis.ValueCondition>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
                .ThrowsAsync(new TimeoutException("redis unavailable"));
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            var svc = new UserService(ctx, ro
                , new Mock<IMiniProgram>().Object
                , redisMock.Object
                , Microsoft.Extensions.Options.Options.Create(new Jyz.Options.JwtSettings { Expires = 60 })
                , new Mock<ILogger<UserService>>().Object
            );

            var act = () => svc.UpdateAsync(1, new UpdateUserRequest { Status = (int)DataStatus.Disable });

            await act.Should().NotThrowAsync();
        }

        // ─── ChangePasswordAsync ──────────────────────────────────────────────

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ThrowsBusinessException()
        {
            var svc = CreateService();

            var act = async () => await svc.ChangePasswordAsync(999,
                new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongOldPassword_ThrowsBusinessException()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", Password = BCrypt.Net.BCrypt.HashPassword("correct") });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var act = async () => await svc.ChangePasswordAsync(1,
                new ChangePasswordRequest { OldPassword = "wrong", NewPassword = "new" });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.InvalidOldPassword);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidRequest_UpdatesPassword()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", Password = BCrypt.Net.BCrypt.HashPassword("old") });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            var result = await svc.ChangePasswordAsync(1,
                new ChangePasswordRequest { OldPassword = "old", NewPassword = "newpass" });

            result.Should().BeTrue();
            var user = await ctx.Users.FirstAsync(u => u.Id == 1);
            BCrypt.Net.BCrypt.Verify("newpass", user.Password).Should().BeTrue();
        }

        // ─── UpdateProfileAsync ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateProfileAsync_UserNotFound_ThrowsBusinessException()
        {
            var svc = CreateService();

            var act = async () => await svc.UpdateProfileAsync(999, new UpdateProfileRequest());

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidRequest_UpdatesFields()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            await svc.UpdateProfileAsync(1, new UpdateProfileRequest
            {
                NickName = "Nick",
                RealName = "Real",
                Phone = "1234567890",
                Avatar = "http://img",
                Gender = Constants.Gender.Male,
                Province = "BC",
                City = "Vancouver",
                Country = "CA"
            });

            var user = await ctx.Users.FirstAsync(u => u.Id == 1);
            user.NickName.Should().Be("Nick");
            user.RealName.Should().Be("Real");
            user.Phone.Should().Be("1234567890");
            user.Gender.Should().Be(Constants.Gender.Male);
            user.City.Should().Be("Vancouver");
        }

        [Fact]
        public async Task UpdateProfileAsync_NullFields_DoesNotOverwrite()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.Users.Add(new User { Id = 1, UserName = "u", NickName = "Original" });
            await ctx.SaveChangesAsync();
            var svc = CreateSvc(ctx, ro);

            // NickName not provided → should not change
            await svc.UpdateProfileAsync(1, new UpdateProfileRequest { RealName = "New" });

            var user = await ctx.Users.FirstAsync(u => u.Id == 1);
            user.NickName.Should().Be("Original");
            user.RealName.Should().Be("New");
        }
    }
}

