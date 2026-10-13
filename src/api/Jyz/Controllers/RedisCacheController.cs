using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Model.RedisCache;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.RedisCache, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class RedisCacheController(IRedisCacheService redisCacheService) : CustomController
    {
        private readonly IRedisCacheService _redisCacheService = redisCacheService;

        [HttpGet]
        [Permission(PermissionCodes.RedisCacheGet)]
        public async Task<IActionResult> GetAllAsync()
        {
            var items = await _redisCacheService.GetAllAsync();
            return Ok(items);
        }

        [HttpDelete]
        [Permission(PermissionCodes.RedisCacheDelete)]
        public async Task<IActionResult> DeleteAsync([FromBody][Required] DeleteRedisCacheRequest request)
        {
            if (request.Keys.Count == 0)
                return Ok();
            await _redisCacheService.DeleteAsync(request.Keys);
            return Ok();
        }
    }
}
