using Mapster;
using Microsoft.AspNetCore.Mvc;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Model;
using Jyz.Model.LoginLog;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.LoginLog, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class LoginLogController(ILoginLogService loginLogService) : CustomController
    {
        private readonly ILoginLogService _loginLogService = loginLogService;

        [HttpGet]
        [Permission(PermissionCodes.LoginLogGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryLoginLogRequest query)
        {
            var result = await _loginLogService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<LoginLogResponse>>());
        }
    }
}
