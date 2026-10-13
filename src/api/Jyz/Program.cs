using Jyz.Extensions;
using Jyz.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Jyz.Constants;
using Jyz.Model;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Application Start");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddNLog();
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter();
    });

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks()
        .AddCheck<Jyz.Infrastructure.DbHealthCheck>("database")
        ;
    builder.Services.AddCustomCoreAPI()
        .AddCustomLocalization()
        .AddCustomCors()
        .AddCustomApiVersioning()
        .AddCustomFluentValidation()
        .AddMapster()
        .AddCustomOpenTelemetry()
        .AddCustomNLog()
        .AddCustomOptions(builder.Configuration)
        .AddCustomAuthentication(builder.Configuration)
        .AddCustomInjection(builder.Configuration);

    var app = builder.Build();

    app.UseCors("AllowAll");

    app.UseRequestLocalization();

    app.UseHttpLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference(options =>
        {
            var descriptions = app.DescribeApiVersions();
            for (var i = 0; i < descriptions.Count; i++)
            {
                var description = descriptions[i];
                var isDefault = i == 0;
                options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
            }
        });
    }
    app.UseAuthentication();
    app.UseBlacklistMiddleware();
    app.UseAuthorization();

    app.UseCustomExceptionHandler();

    app.UsePermissionSync();

    app.UseOpenTelemetryPrometheusScrapingEndpoint();

    app.MapHealthChecks("/api/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new BaseResponse<object>
            {
                Data = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration
                    }),
                    totalDuration = report.TotalDuration
                }
            };

            if (report.Status != HealthStatus.Healthy)
            {
                response.Code = ResponseCode.Failed;
                response.Message = ResponseCode.Failed.GetDescription();
            }

            await context.Response.WriteAsJsonAsync(response);
        }
    });

    app.MapControllers();

    await app.RunAsync();
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    // Thrown by WebApplicationBuilder.Build() when EF Core design-time tools (dotnet ef migrations
    // add/database update, etc.) introspect the app to resolve the DbContext. Must propagate
    // unhandled so the EF tooling process can catch it — do not log or Environment.Exit here.
    throw;
}
catch (Exception ex)
{
    logger.Error(ex, "Application start failed");
    LogManager.Shutdown();
    Environment.Exit(1);
}
