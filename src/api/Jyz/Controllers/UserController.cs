using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.Request;
using Jyz.Model.Response;
using Jyz.Model.User;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.User, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class UserController(IUserService userService, ICurrentUser currentUser) : CustomController
    {
        private readonly IUserService _userService = userService;
        private readonly ICurrentUser _currentUser = currentUser;

        [HttpGet]
        [Permission(PermissionCodes.UserGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryRequest query)
        {
            var result = await _userService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<UserResponse>>());
        }

        [HttpGet("{id}")]
        [Permission(PermissionCodes.UserGet)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var user = await _userService.GetOneAsync(q => q.Id.Equals(id));
            if (user == null)
                return BadRequest(ErrorCode.UserNotFound);
            return Ok(user.Adapt<UserResponse>());
        }

        [HttpPut("{id}")]
        [Permission(PermissionCodes.UserEdit)]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody][Required] UpdateUserRequest request)
        {

            if (await _userService.UpdateAsync(id, request))
                return Ok();
            else
                return BadRequest(ErrorCode.OperationFailed);
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var user = await _userService.GetOneAsync(q => q.Id.Equals(_currentUser.Id));
            if (user == null)
                return BadRequest(ErrorCode.UserNotFound);
            return Ok(user.Adapt<UserResponse>());
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync([FromBody][Required] ChangePasswordRequest request)
        {
            if (!await _userService.ChangePasswordAsync(_currentUser.Id ?? 0, request))
                return BadRequest(ErrorCode.OperationFailed);
            return Ok();
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfileAsync([FromBody][Required] UpdateProfileRequest request)
        {
            if (!await _userService.UpdateProfileAsync(_currentUser.Id ?? 0, request))
                return BadRequest(ErrorCode.OperationFailed);
            return Ok();
        }
    }
}
