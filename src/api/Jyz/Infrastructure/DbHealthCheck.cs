using Jyz.Entity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jyz.Infrastructure
{
    /// <summary>
    /// Probes the configured database through <see cref="AppDbContext"/>.
    /// Works with any provider supported by JfYu.Data (SqlServer, MySql, MariaDB, PostgreSQL).
    /// </summary>
    public class DbHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Database is unreachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed.", ex);
            }
        }
    }
}
