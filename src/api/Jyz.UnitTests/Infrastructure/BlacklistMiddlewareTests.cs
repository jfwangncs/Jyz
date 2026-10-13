using System.Security.Claims;
using JfYu.Redis.Interface;
using Jyz.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RedisKey = StackExchange.Redis.RedisKey;

namespace Jyz.UnitTests.Infrastructure
{
    public class BlacklistMiddlewareTests
    {
        private static DefaultHttpContext BuildHttpContext(int? userId, bool authenticated, bool allowAnonymous = false)
        {
            var http = new DefaultHttpContext();

            if (authenticated && userId.HasValue)
            {
                var identity = new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                    "test");
                http.User = new ClaimsPrincipal(identity);
            }
            else if (authenticated)
            {
                http.User = new ClaimsPrincipal(new ClaimsIdentity("test"));
            }

            if (allowAnonymous)
            {
                var endpoint = new Endpoint(
                    requestDelegate: null,
                    metadata: new EndpointMetadataCollection(new AllowAnonymousAttribute()),
                    displayName: "anon");
                http.SetEndpoint(endpoint);
            }

            var jsonOptions = Microsoft.Extensions.Options.Options.Create(new JsonOptions());
            http.RequestServices = new ServiceCollection()
                .AddSingleton<IOptions<JsonOptions>>(jsonOptions)
                .BuildServiceProvider();

            http.Response.Body = new MemoryStream();
            return http;
        }

        [Fact]
        public async Task PassesThrough_WhenUserNotAuthenticated()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var redisMock = new Mock<IRedisService>();

            var middleware = new BlacklistMiddleware(next, NullLogger<BlacklistMiddleware>.Instance);
            await middleware.InvokeAsync(BuildHttpContext(null, authenticated: false), redisMock.Object);

            nextCalled.Should().BeTrue();
            redisMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PassesThrough_WhenEndpointAllowsAnonymous()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            var dbMock = new Mock<IDatabase>();
            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);

            var middleware = new BlacklistMiddleware(next, NullLogger<BlacklistMiddleware>.Instance);
            await middleware.InvokeAsync(
                BuildHttpContext(1, authenticated: true, allowAnonymous: true),
                redisMock.Object);

            nextCalled.Should().BeTrue();
            dbMock.Verify(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
        }

        [Fact]
        public async Task PassesThrough_WhenUserNotBlacklisted()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);
            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);

            var middleware = new BlacklistMiddleware(next, NullLogger<BlacklistMiddleware>.Instance);
            await middleware.InvokeAsync(BuildHttpContext(1, authenticated: true), redisMock.Object);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Returns401_WhenUserBlacklisted()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);

            var ctx = BuildHttpContext(1, authenticated: true);
            var middleware = new BlacklistMiddleware(next, NullLogger<BlacklistMiddleware>.Instance);
            await middleware.InvokeAsync(ctx, redisMock.Object);

            nextCalled.Should().BeFalse();
            ctx.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            ctx.Response.ContentType.Should().Contain("application/json");
        }

        [Fact]
        public async Task SwallowsException_AndPassesThrough_WhenRedisFails()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, It.IsAny<CommandFlags>(), "boom"));
            var redisMock = new Mock<IRedisService>();
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);

            var middleware = new BlacklistMiddleware(next, NullLogger<BlacklistMiddleware>.Instance);
            await middleware.InvokeAsync(BuildHttpContext(1, authenticated: true), redisMock.Object);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public void UseBlacklistMiddleware_RegistersMiddlewareInPipeline()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<IRedisService>().Object);
            services.AddLogging();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());

            var result = appBuilder.UseBlacklistMiddleware();

            result.Should().BeSameAs(appBuilder);
            var pipeline = appBuilder.Build();
            pipeline.Should().NotBeNull();
        }
    }
}
