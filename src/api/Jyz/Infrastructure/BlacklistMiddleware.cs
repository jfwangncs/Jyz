using System.Security.Claims;
using JfYu.Redis.Interface;
using Jyz.Constants;
using Jyz.Extensions;
using Jyz.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jyz.Infrastructure
{
    public class BlacklistMiddleware(RequestDelegate next, ILogger<BlacklistMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<BlacklistMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context, IRedisService redisService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var allowAnonymous = context.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().Any() ?? false;
                if (!allowAnonymous)
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        try
                        {
                            var blacklistKey = string.Format(RedisKey.UserBlacklist, userId);
                            var isBlacklisted = await redisService.Database.KeyExistsAsync(blacklistKey);
                            if (isBlacklisted)
                            {
                                var jsonOptions = context.RequestServices
                                    .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsJsonAsync(new BaseResponse<string>
                                {
                                    Code = ResponseCode.Failed,
                                    ErrorCode = ErrorCode.AccountDisabled,
                                    Message = ErrorCode.AccountDisabled.GetDescription()
                                }, jsonOptions, context.RequestAborted);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Redis unavailable, skipping blacklist check for user {UserId}", userId);
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    public static class BlacklistMiddlewareExtensions
    {
        public static IApplicationBuilder UseBlacklistMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<BlacklistMiddleware>();
    }
}
