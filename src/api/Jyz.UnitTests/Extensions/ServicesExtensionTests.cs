using Jyz.Constants;
using Jyz.Extensions;
using Jyz.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Jyz.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace Jyz.UnitTests.Extensions
{
    public class ServicesExtensionTests
    {
        private static IConfiguration BuildJwtConfiguration() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "unit-test-secret-key-at-least-32-chars",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:Expires"] = "60",
            })
            .Build();

        // ─── AddCustomCoreAPI ────────────────────────────────────────────────

        [Fact]
        public void AddCustomCoreAPI_RegistersCurrentUserAndHttpContextAccessor()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddCustomCoreAPI();
            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().Should().NotBeNull();
            scope.ServiceProvider.GetRequiredService<Jyz.Services.Interfaces.ICurrentUser>().Should().BeOfType<Jyz.Services.CurrentUser>();

            // Resolving IOptions<T>.Value forces the AddJsonOptions/ConfigureHttpJsonOptions
            // configure delegates to actually run (they're deferred, not invoked at registration time).
            var mvcJsonOptions = scope.ServiceProvider.GetRequiredService<IOptions<JsonOptions>>().Value;
            mvcJsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
            mvcJsonOptions.JsonSerializerOptions.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);

            var httpJsonOptions = scope.ServiceProvider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value;
            httpJsonOptions.SerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
            httpJsonOptions.SerializerOptions.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
        }

        [Fact]
        public void AddCustomCoreAPI_InvalidModelStateResponseFactory_ReturnsValidationErrorBadRequest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCustomCoreAPI();
            var provider = services.BuildServiceProvider();

            var apiBehaviorOptions = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var factory = apiBehaviorOptions.InvalidModelStateResponseFactory;

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "Name is required");
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);

            var result = factory(actionContext);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var body = badRequest.Value.Should().BeOfType<BaseResponse<Dictionary<string, ModelErrorCollection>>>().Subject;
            body.Code.Should().Be(ResponseCode.Failed);
            body.ErrorCode.Should().Be(ErrorCode.ValidationError);
            body.Data.Should().ContainKey("Name");
        }

        // ─── AddCustomApiVersioning ──────────────────────────────────────────

        [Fact]
        public void AddCustomApiVersioning_DoesNotThrow_AndBuildsProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var act = () =>
            {
                services.AddCustomApiVersioning();
                services.BuildServiceProvider();
            };

            act.Should().NotThrow();
        }

        // ─── AddCustomAuthentication ─────────────────────────────────────────

        [Fact]
        public async Task AddCustomAuthentication_RegistersJwtBearerAsDefaultScheme()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddCustomAuthentication(BuildJwtConfiguration());
            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
            defaultScheme!.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        }

        // ─── AddCustomOpenTelemetry ──────────────────────────────────────────

        [Fact]
        public void AddCustomOpenTelemetry_DoesNotThrow_AndBuildsProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var act = () =>
            {
                services.AddCustomOpenTelemetry();
                services.BuildServiceProvider();
            };

            act.Should().NotThrow();
        }

        // ─── AddCustomNLog / AddCustomCors / AddCustomLocalization / AddCustomFluentValidation ──

        [Fact]
        public void AddCustomNLog_DoesNotThrow_AndBuildsProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var act = () =>
            {
                services.AddCustomNLog();
                services.BuildServiceProvider();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void AddCustomCors_RegistersCorsService()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddCustomCors();
            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>().Should().NotBeNull();
        }

        [Fact]
        public void AddCustomLocalization_RegistersRequestLocalizationOptions()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddCustomLocalization();
            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>>().Value;
            options.SupportedCultures.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void AddCustomFluentValidation_DoesNotThrow_AndBuildsProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCustomCoreAPI();

            var act = () =>
            {
                services.AddCustomFluentValidation();
                services.BuildServiceProvider();
            };

            act.Should().NotThrow();
        }

        // ─── UsePermissionSync ───────────────────────────────────────────────

        [Fact]
        public void UsePermissionSync_InvokesSyncAndGrantOnPermissionService()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Path.GetTempPath()
            });
            var permissionServiceMock = new Mock<IPermissionService>();
            builder.Services.AddSingleton(permissionServiceMock.Object);
            using var app = builder.Build();

            app.UsePermissionSync();

            permissionServiceMock.Verify(s => s.SyncAsync(), Times.Once);
            permissionServiceMock.Verify(s => s.GrantAllPermissionsToDefaultAdminRole(), Times.Once);
        }
    }
}
