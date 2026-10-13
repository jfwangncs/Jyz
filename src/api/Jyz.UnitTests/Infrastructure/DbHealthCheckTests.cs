using Jyz.Infrastructure;
using Jyz.UnitTests.TestBase;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Infrastructure
{
    public class DbHealthCheckTests
    {
        private static (DbHealthCheck healthCheck, Entity.AppDbContext ctx) CreateHealthCheck()
        {
            var (ctx, _) = DbContextFactory.CreateInMemory();

            var services = new ServiceCollection();
            services.AddSingleton(ctx);
            var provider = services.BuildServiceProvider();

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            return (new DbHealthCheck(scopeFactoryMock.Object), ctx);
        }

        [Fact]
        public async Task CheckHealthAsync_DatabaseConnectable_ReturnsHealthy()
        {
            var (healthCheck, _) = CreateHealthCheck();

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Fact]
        public async Task CheckHealthAsync_DatabaseThrows_ReturnsUnhealthy()
        {
            var (healthCheck, ctx) = CreateHealthCheck();
            await ctx.DisposeAsync();

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }
    }
}
