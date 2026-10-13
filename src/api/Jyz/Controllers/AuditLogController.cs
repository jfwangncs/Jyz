using Mapster;
using Microsoft.AspNetCore.Mvc;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Model;
using Jyz.Model.AuditLog;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{  
    [Permission(PermissionCodes.AuditLog, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class AuditLogController(IAuditLogService auditLogService) : CustomController
    {
        private readonly IAuditLogService _auditLogService = auditLogService;

        [HttpGet]
        [Permission(PermissionCodes.AuditLogGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryAuditLogRequest query)
        {
            var result = await _auditLogService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<AuditLogResponse>>());
        }
    }
}
