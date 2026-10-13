using Mapster;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Jyz.Attributes;
using Jyz.Constants;
using Jyz.Entity;
using Jyz.Exceptions;
using Jyz.Model;
using Jyz.Model.DictItem;
using Jyz.Services.Interfaces;

namespace Jyz.Controllers
{ 
    [Permission(PermissionCodes.DictType, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class DictItemController(IDictItemService dictItemService, IDictTypeService dictTypeService) : CustomController
    {
        private readonly IDictItemService _dictItemService = dictItemService;
        private readonly IDictTypeService _dictTypeService = dictTypeService;

        [HttpGet]
        [Permission(PermissionCodes.DictItemGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryDictItemRequest query)
        {
            var result = await _dictItemService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<DictItemResponse>>());
        }

        [HttpPost]
        [Permission(PermissionCodes.DictItemAdd)]
        public async Task<IActionResult> CreateAsync([FromBody][Required] CreateDictItemRequest request)
        {
            var dictType = await _dictTypeService.GetOneAsync(q => q.Id == request.DictTypeId);
            if (dictType == null)
                return BadRequest(ErrorCode.DictTypeNotFound);

            if (await _dictItemService.IsCodeDuplicateAsync(request.DictTypeId, request.Code))
                throw new BusinessException(ErrorCode.DuplicateDictItemCode);

            var item = request.Adapt<DictItem>();
            item.Status = 1;
            await _dictItemService.AddAsync(item);
            return Ok();
        }

        [HttpPut("{id}")]
        [Permission(PermissionCodes.DictItemEdit)]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody][Required] UpdateDictItemRequest request)
        {
            var item = await _dictItemService.GetOneAsync(q => q.Id == id);
            if (item == null)
                return BadRequest(ErrorCode.DictItemNotFound);

            if (request.Label != null) item.Label = request.Label;
            if (request.Sort.HasValue) item.Sort = request.Sort.Value;
            if (request.Status.HasValue) item.Status = request.Status.Value;

            await _dictItemService.UpdateAsync(item);
            return Ok();
        }
    }
}
