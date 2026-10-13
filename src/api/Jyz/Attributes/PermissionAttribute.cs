using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Jyz.Constants;
using Jyz.Extensions;
using Jyz.Model;
using Jyz.Entity;
using JfYu.Redis.Interface;

namespace Jyz.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PermissionAttribute(string code, PermissionType type = PermissionType.Button, string? parentCode = null) : Attribute, IAsyncAuthorizationFilter
    {
        public string Code { get; } = code;
        public PermissionType Type { get; } = type;
        public string? ParentCode { get; } = parentCode;

        private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(30);

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // skip [AllowAnonymous]
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
                return;

            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ObjectResult(new BaseResponse<string>
                {
                    Code = ResponseCode.Error,
                    Message = ErrorCode.UnauthorizedError.GetDescription(),
                    ErrorCode = ErrorCode.UnauthorizedError,
                })
                { StatusCode = 401 };
                return;
            }

            if (Type == PermissionType.Menu)
                return;

            var permissions = await GetPermissionsAsync(context.HttpContext, user);

            if (!permissions.Contains(Code))
            {
                context.Result = new ObjectResult(new BaseResponse<string>
                {
                    Code = ResponseCode.Error,
                    Message = ErrorCode.ForbiddenError.GetDescription(),
                    ErrorCode = ErrorCode.ForbiddenError,
                })
                { StatusCode = 403 };
            }
        }

        private static async Task<HashSet<string>> GetPermissionsAsync(HttpContext httpContext, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var redis = httpContext.RequestServices.GetService<IRedisService>();
            if (redis != null && userId != null)
            {
                var logger = httpContext.RequestServices.GetService<ILogger<PermissionAttribute>>();
                var cacheKey = string.Format(RedisKey.UserPermission, userId);

                List<string>? cached = null;
                try
                {
                    cached = await redis.GetAsync<List<string>>(cacheKey);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Redis unavailable, falling back to database for user {UserId} permissions", userId);
                }
                if (cached != null)
                    return [.. cached];

                var db = httpContext.RequestServices.GetService<AppDbContext>();
                if (db != null && int.TryParse(userId, out var uid))
                {
                    var dbPerms = await db.Users
                        .Where(u => u.Id == uid)
                        .SelectMany(u => u.Roles)
                        .SelectMany(r => r.Permissions)
                        .Where(p => p.Status == 1 && p.Type == PermissionType.Button)
                        .Select(p => p.Code)
                        .Distinct()
                        .ToListAsync(httpContext.RequestAborted);

                    try
                    {
                        await redis.AddAsync(cacheKey, dbPerms, PermissionCacheTtl);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Redis unavailable, skipping permission cache write for user {UserId}", userId);
                    }
                    return [.. dbPerms];
                }
            }

            // fallback: read from JWT claims
            return user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet();
        }
    }
}
