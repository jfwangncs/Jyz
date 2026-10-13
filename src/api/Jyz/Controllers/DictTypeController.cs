using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Exceptions;
using Jyz.Model;
using Jyz.Model.DictType;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.DictType, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class DictTypeController(IDictTypeService dictTypeService) : CustomController
    {
        private readonly IDictTypeService _dictTypeService = dictTypeService;

        [HttpGet]
        [Permission(PermissionCodes.DictTypeGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryDictTypeRequest query)
        {
            var result = await _dictTypeService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<DictTypeResponse>>());
        }

        [HttpGet("{id}")]
        [Permission(PermissionCodes.DictTypeGet)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var item = await _dictTypeService.GetByIdWithItemsAsync(id);
            if (item == null)
                return BadRequest(ErrorCode.DictTypeNotFound);
            return Ok(item.Adapt<DictTypeResponse>());
        }

        [HttpGet("lookup")]
        [Authorize]
        public async Task<IActionResult> LookupAsync()
        {
            var items = await _dictTypeService.GetAllWithItemsAsync();
            return Ok(items.Adapt<List<DictTypeResponse>>());
        }

        [HttpPost]
        [Permission(PermissionCodes.DictTypeAdd)]
        public async Task<IActionResult> CreateAsync([FromBody][Required] CreateDictTypeRequest request)
        {
            if (await _dictTypeService.IsCodeDuplicateAsync(request.Code))
                throw new BusinessException(ErrorCode.DuplicateDictTypeCode);

            var item = request.Adapt<DictType>();
            item.Status = 1;
            await _dictTypeService.AddAsync(item);
            return Ok();
        }

        [HttpPut("{id}")]
        [Permission(PermissionCodes.DictTypeEdit)]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody][Required] UpdateDictTypeRequest request)
        {
            var item = await _dictTypeService.GetOneAsync(q => q.Id == id);
            if (item == null)
                return BadRequest(ErrorCode.DictTypeNotFound);

            if (request.Name != null) item.Name = request.Name;
            if (request.Description != null) item.Description = request.Description;
            if (request.Status.HasValue) item.Status = request.Status.Value;

            await _dictTypeService.UpdateAsync(item);
            return Ok();
        }
    }
}
