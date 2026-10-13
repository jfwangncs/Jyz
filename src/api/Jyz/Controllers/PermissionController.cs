using Mapster;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Model.Permission;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.Permission, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class PermissionController(IPermissionService permissionService) : CustomController
    {
        private readonly IPermissionService _permissionService = permissionService;

        [HttpGet("list")]
        [Permission(PermissionCodes.PermissionGet)]
        public async Task<IActionResult> GetListAsync()
        {
            var result = await _permissionService.GetListAsync();
            return Ok(result.Adapt<List<PermissionResponse>>());
        }

        [HttpGet("{id}")]
        [Permission(PermissionCodes.PermissionGet)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var permission = await _permissionService.GetOneAsync(q => q.Id.Equals(id));
            if (permission == null)
                return BadRequest(ErrorCode.PermissionNotFound);
            return Ok(permission.Adapt<PermissionResponse>());
        }

        [HttpPost]
        [Permission(PermissionCodes.PermissionAdd)]
        public async Task<IActionResult> CreateAsync([FromBody][Required] CreatePermissionRequest request)
        {
            var exists = await _permissionService.GetOneAsync(q => q.Code == request.Code);
            if (exists != null)
                return BadRequest(ErrorCode.DuplicatePermission);

            var permission = request.Adapt<Permission>();
            if (await _permissionService.AddAsync(permission) > 0)
                return Ok(permission.Adapt<PermissionResponse>());
            else
                return BadRequest(ErrorCode.OperationFailed);
        }

        [HttpPut("{id}")]
        [Permission(PermissionCodes.PermissionEdit)]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody][Required] UpdatePermissionRequest request)
        {
            var permission = await _permissionService.GetOneAsync(q => q.Id.Equals(id));
            if (permission == null)
                return BadRequest(ErrorCode.PermissionNotFound);

            if (await _permissionService.UpdateAsync(request.Adapt(permission)) > 0)
                return Ok(permission.Adapt<PermissionResponse>());
            else
                return BadRequest(ErrorCode.OperationFailed);
        }

        [HttpPost("sync")]
        [Permission(PermissionCodes.PermissionSync)]
        public IActionResult Sync()
        {
            _permissionService.SyncAsync();
            return Ok();
        }
    }
}
