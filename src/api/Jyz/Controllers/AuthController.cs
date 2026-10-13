using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Extensions;
using Jyz.Model;
using Jyz.Model.Response;
using Jyz.Model.User;
using Jyz.Options;
using Jyz.Services.Interfaces;
using JfYu.Data.Model;
using Jyz.Exceptions;

namespace Jyz.Controllers
{ 
    public class AuthController(IJwtService jwtService, IUserService userService, IOptions<JwtSettings> jwtSettings, ICurrentUser currentUser
        , ILoginLogService loginLogService
        ) : CustomController
    {
        private readonly IJwtService _jwtService = jwtService;
        private readonly IUserService _userService = userService;
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILoginLogService _loginLogService = loginLogService;

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<LoginResponse>), 200)]
        public async Task<IActionResult> LoginAsync([FromBody][Required] LoginRequest request)
        {
            try
            {
                User? user = await _userService.LoginAsync(request);
                if (user == null)
                    return BadRequest(ErrorCode.InvalidCredentials);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.UserName),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                foreach (var item in user.Roles)
                    claims.Add(new Claim(ClaimTypes.Role, item.Name));

                foreach (var permission in user.Roles.Where(q => q.Status == (int)DataStatus.Active).SelectMany(r => r.Permissions).Where(p => p.Status == (int)DataStatus.Active).Select(p => p.Code).Distinct().ToList())
                    claims.Add(new Claim("permission", permission));

                var token = _jwtService.GenerateToken(claims);

                var response = new LoginResponse
                {
                    Id = user.Id,
                    AccessToken = token,
                    RealName = user.RealName ?? user.UserName,
                    ExpiresIn = _jwtSettings.Expires,
                    //why return permissions as roles? because frontend have fixed filed "roles" to verify whether user have access right, if return "permissions", frontend need to change a lot of code, and "roles" is more intuitive for frontend to use
                    Roles = [.. user.Roles.Where(q => q.Status == (int)DataStatus.Active).SelectMany(r => r.Permissions).Where(p => p.Status == (int)DataStatus.Active).Select(p => p.Code).Distinct()]
                };

                await _loginLogService.AddAsync(new LoginLog
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Result = (int)LoginResult.Success,
                    Platform = (int)request.Platform
                });

                return Ok(response);
            }
            catch (BusinessException ex)
            {
                var loginResult = ex.ErrorCode switch
                {
                    ErrorCode.AccountDisabled => LoginResult.AccountDisabled,
                    ErrorCode.AccountLocked => LoginResult.AccountLocked,
                    _ => LoginResult.Failed
                };
                await _loginLogService.AddAsync(new LoginLog
                {
                    UserName = request.UserName,
                    IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Result = (int)loginResult,
                    FailReason = ex.ErrorCode.GetDescription(),
                    Platform = (int)request.Platform
                });
                throw;
            }
        }

        [HttpGet("codes")]
        [Authorize]
        public async Task<IActionResult> GetAccessCode()
        {
            var user = await _userService.GetOneAsync(q => q.Id.Equals(_currentUser.Id));
            if (user == null)
                return BadRequest(ErrorCode.UserNotFound);

            return Ok(user.Roles.Where(q => q.Status == (int)DataStatus.Active).SelectMany(r => r.Permissions).Where(p => p.Status == (int)DataStatus.Active).Select(p => p.Code).Distinct().ToList());
        }
    }
}
