using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Jyz.Constants;
using Jyz.Resources;
using Jyz.Exceptions;
using Jyz.Model;
using Jyz.Options;
using Jyz.Services;
using Jyz.Services.Interfaces;

namespace Jyz.Extensions
{
    public static class ServicesExtension
    {
        public static readonly string ServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "WebApi";

        public static IServiceCollection AddCustomCoreAPI(this IServiceCollection services)
        {
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errorMsg = new Dictionary<string, ModelErrorCollection>();
                    if (!context.ModelState.IsValid)
                    {
                        var errors = context.ModelState.Where(q => q.Value != null && q.Value.Errors.Count > 0);
                        foreach (var error in errors)
                        {
                            if (error.Value != null)
                                errorMsg.Add(error.Key, error.Value.Errors);
                        }
                    }
                    return new BadRequestObjectResult(new BaseResponse<Dictionary<string, ModelErrorCollection>>() { Code = ResponseCode.Failed, ErrorCode = ErrorCode.ValidationError, Message = ErrorCode.ValidationError.GetDescription(), Data = errorMsg });
                };

            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            return services;
        }

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            // API Versioning
            services.AddApiVersioning(options =>
            {
                // Controllers intentionally rely on the default API version (1.0) when the client omits it,
                // so AV0016 ("setting is only necessary for APIs without an explicit version") is expected.
#pragma warning disable AV0016
                options.AssumeDefaultVersionWhenUnspecified = true;
#pragma warning restore AV0016
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                     new QueryStringApiVersionReader("api-version"),
                     new HeaderApiVersionReader("x-api-version"));
            }).AddMvc().AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            }).AddOpenApi(options =>
            {
                var openApi = options.Document;
                openApi.AddSchemaTransformer((schema, context, cancellationToken) =>
                {
                    var type = context.JsonTypeInfo.Type;
                    var enumType = Nullable.GetUnderlyingType(type) ?? type;
                    if (enumType.IsEnum)
                    {
                        schema.Type = JsonSchemaType.Integer;
                        schema.Enum = [.. Enum.GetValues(enumType)
                            .Cast<int>()
                            .Select(v => (JsonNode)v)];
                        var descriptions = new List<string>();
                        foreach (var value in Enum.GetValues(enumType))
                        {
                            var intValue = Convert.ToInt32(value);
                            var name = Enum.GetName(enumType, value) ?? string.Empty;
                            var description = GetEnumDescription(enumType, name);

                            if (!string.IsNullOrEmpty(description))
                                descriptions.Add($"{intValue} = {name} ({description})");
                            else
                                descriptions.Add($"{intValue} = {name}");
                        }

                        schema.Description = string.Join(", ", descriptions);
                    }
                    return Task.CompletedTask;
                });
                openApi.AddDocumentTransformer((document, context, cancellationToken) =>
                {

                    document.Components ??= new();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                    // Define JWT Bearer scheme so Scalar can collect a token
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "Input your JWT. The UI will send 'Authorization: Bearer {token}'."
                    };

                    return Task.CompletedTask;
                });
            });
            return services;
            static string GetEnumDescription(Type enumType, string enumName)
            {
                var key = $"{enumType.Name}.{enumName}";
                var localized = EnumResources.GetString(key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(localized))
                    return localized;

                var memberInfo = enumType.GetMember(enumName).FirstOrDefault();
                var descriptionAttribute = memberInfo?.GetCustomAttribute<DescriptionAttribute>();
                return descriptionAttribute?.Description ?? string.Empty;
            }
        }

        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
               .ConfigureResource(resource => resource
                   .AddService(ServiceName)
                   .AddAttributes(new Dictionary<string, object>
                   {
                       ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                   }))
               .WithTracing(tracing => tracing
                    .AddSource(ServiceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = (httpContext) =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
                            return !path.Contains("/health")
                                && !path.Contains("/metrics")
                                && !path.StartsWith("/scalar")
                                && !path.StartsWith("/openapi");
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRabbitMQInstrumentation()
                    .AddRedisInstrumentation()
                    .AddOtlpExporter())
               .WithMetrics(metrics => metrics
                    .AddMeter(ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter());
            return services;
        }

        public static IServiceCollection AddCustomNLog(this IServiceCollection services)
        {

            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.All;
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
                logging.CombineLogs = true;
            });
            services.AddRedaction();
#pragma warning disable EXTEXP0013
            services.AddHttpLoggingRedaction(op =>
            {
                op.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None;
                op.RequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
                op.ExcludePathStartsWith.Add("/scalar");
                op.ExcludePathStartsWith.Add("/openapi");
                op.ExcludePathStartsWith.Add("/metrics");
                op.ExcludePathStartsWith.Add("/health");
                op.IncludeUnmatchedRoutes = true;
            });
#pragma warning restore EXTEXP0013
            return services;
        }

        public static IServiceCollection AddCustomFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddFluentValidationAutoValidation(options => options.DisableDataAnnotationsValidation = true);
            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var JwtConfig = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = JwtConfig?.Audience,
                    ValidIssuer = JwtConfig?.Issuer,
                    RequireExpirationTime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtConfig?.SecretKey!))
                };
                options.Events = new JwtBearerEvents()
                {
                    OnForbidden = async context =>
                    {
                        var allowAnonymousAttribute = context.HttpContext.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().FirstOrDefault();
                        if (allowAnonymousAttribute == null)
                        {
                            context.NoResult();
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync(JsonSerializer.Serialize(new BaseResponse<string>()
                            {
                                Code = ResponseCode.Failed,
                                ErrorCode = ErrorCode.ForbiddenError,
                                Message = ErrorCode.ForbiddenError.GetDescription()
                            }));
                        }
                    },
                    OnChallenge = async context =>
                    {
                        var allowAnonymousAttribute = context.HttpContext.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().FirstOrDefault();
                        if (allowAnonymousAttribute == null)
                        {
                            context.HandleResponse();
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync(JsonSerializer.Serialize(new BaseResponse<string>()
                            {
                                Code = ResponseCode.Failed,
                                ErrorCode = ErrorCode.UnauthorizedError,
                                Message = ErrorCode.UnauthorizedError.GetDescription()
                            }));
                        }
                    },
                };
            });

            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
            return services;
        }

        public static IServiceCollection AddCustomLocalization(this IServiceCollection services)
        {
            services.AddLocalization();
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("zh-CN") };
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
            return services;
        }

        public static void UseCustomExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error is BusinessException businessEx)
                    {
                        context.Response.StatusCode = (int)businessEx.StatusCode;
                        await context.Response.WriteAsJsonAsync(new BaseResponse<string>()
                        {
                            Code = ResponseCode.Failed,
                            Message = businessEx.ErrorCode.GetDescription(),
                            ErrorCode = businessEx.ErrorCode,
                        }, jsonOptions);
                    }
                    else
                    {
                        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                        if (exceptionHandlerPathFeature != null)
                        {
                            var exception = exceptionHandlerPathFeature.Error;
                            var errorMessage = exceptionHandlerPathFeature.Error.InnerException?.Message ?? exceptionHandlerPathFeature.Error.Message;
                            logger.LogError(exception, "GlobalException: {ErrorMessage}", errorMessage);
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new BaseResponse<string>()
                            {
                                Code = ResponseCode.Error,
                                Message = ErrorCode.SystemError.GetDescription(),
                                ErrorCode = ErrorCode.SystemError,
                            }, jsonOptions);
                        }
                    }
                });
            });
        }

        public static void UsePermissionSync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            syncService.SyncAsync();
            syncService.GrantAllPermissionsToDefaultAdminRole();
        }
    }
}
