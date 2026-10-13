using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Exceptions;
using Jyz.Extensions;
using Jyz.Model;
using Jyz.Model.DictItem;
using Jyz.Model.DictType;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jyz.UnitTests.Controllers
{
    public class DictTypeControllerTests
    {
        private static (DictTypeController ctrl, Mock<IDictTypeService> svcMock) CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            var svcMock = new Mock<IDictTypeService>();
            var ctrl = new DictTypeController(svcMock.Object);
            return (ctrl, svcMock);
        }

        private static BaseResponse<object>? GetOkValue(IActionResult result)
            => result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        private static BaseResponse<object>? GetBadValue(IActionResult result)
            => result.Should().BeOfType<BadRequestObjectResult>().Subject.Value as BaseResponse<object>;

        [Fact]
        public async Task GetAllAsync_ReturnsOk()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryDictTypeRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<DictType> { Data = [], TotalCount = 0 });

            var result = await ctrl.GetAllAsync(new QueryDictTypeRequest());

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsOk()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetByIdWithItemsAsync(1)).ReturnsAsync(new DictType { Id = 1, Code = "gender" });

            var result = await ctrl.GetByIdAsync(1);

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsBadRequest()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetByIdWithItemsAsync(99)).ReturnsAsync((DictType?)null);

            var result = await ctrl.GetByIdAsync(99);

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DictTypeNotFound);
        }

        [Fact]
        public async Task LookupAsync_ReturnsOkWithList()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetAllWithItemsAsync()).ReturnsAsync([new DictType { Code = "gender" }]);

            var result = await ctrl.LookupAsync();

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task CreateAsync_NoDuplicate_ReturnsOk()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.IsCodeDuplicateAsync("gender")).ReturnsAsync(false);
            svcMock.Setup(s => s.AddAsync(It.IsAny<DictType>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.CreateAsync(new CreateDictTypeRequest { Code = "gender", Name = "Gender" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsBusinessException()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.IsCodeDuplicateAsync("gender")).ReturnsAsync(true);

            var act = async () => await ctrl.CreateAsync(new CreateDictTypeRequest { Code = "gender", Name = "G" });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.DuplicateDictTypeCode);
        }

        [Fact]
        public async Task UpdateAsync_Found_ReturnsOk()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictType, bool>>>(), default))
                .ReturnsAsync(new DictType { Id = 1, Code = "gender", Name = "Gender" });
            svcMock.Setup(s => s.UpdateAsync(It.IsAny<DictType>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.UpdateAsync(1, new UpdateDictTypeRequest { Name = "Gender Updated" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsBadRequest()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictType, bool>>>(), default))
                .ReturnsAsync((DictType?)null);

            var result = await ctrl.UpdateAsync(99, new UpdateDictTypeRequest { Name = "X" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DictTypeNotFound);
        }
    }

    public class DictItemControllerTests
    {
        private static (DictItemController ctrl, Mock<IDictItemService> itemMock, Mock<IDictTypeService> typeMock) CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            var itemMock = new Mock<IDictItemService>();
            var typeMock = new Mock<IDictTypeService>();
            var ctrl = new DictItemController(itemMock.Object, typeMock.Object);
            return (ctrl, itemMock, typeMock);
        }

        private static BaseResponse<object>? GetOkValue(IActionResult result)
            => result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        private static BaseResponse<object>? GetBadValue(IActionResult result)
            => result.Should().BeOfType<BadRequestObjectResult>().Subject.Value as BaseResponse<object>;

        [Fact]
        public async Task GetAllAsync_ReturnsOk()
        {
            var (ctrl, itemMock, _) = CreateController();
            itemMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryDictItemRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<DictItem> { Data = [], TotalCount = 0 });

            var result = await ctrl.GetAllAsync(new QueryDictItemRequest());

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetAllAsync_WithItems_MapsFieldsToResponse()
        {
            try { new ServiceCollection().AddMapster(); }
            catch (InvalidOperationException)
            {
                // Mapster's TypeAdapterConfig.GlobalSettings is a process-wide singleton that other
                // test classes may have already compiled (via Adapt<T>()) when tests run in parallel;
                // once compiled it can no longer accept new Map() registrations. That's fine here —
                // whichever test got there first already configured the PagedData<>→PagedResult<> mapping
                // this test depends on, since AddMapster() always registers the same mappings.
            }
            var (ctrl, itemMock, _) = CreateController();
            var item = new DictItem { Id = 1, DictTypeId = 1, Code = "m", Label = "Male", Sort = 0, Status = 1 };
            itemMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryDictItemRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<DictItem> { Data = [item], TotalCount = 1 });

            var result = await ctrl.GetAllAsync(new QueryDictItemRequest());

            var resp = GetOkValue(result)!;
            var paged = resp.Data.Should().BeOfType<PagedResult<DictItemResponse>>().Subject;
            paged.Items.Should().ContainSingle();
            var mapped = paged.Items[0];
            mapped.Code.Should().Be("m");
            mapped.Label.Should().Be("Male");
            mapped.DictTypeId.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_DictTypeNotFound_ReturnsBadRequest()
        {
            var (ctrl, _, typeMock) = CreateController();
            typeMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictType, bool>>>(), default))
                .ReturnsAsync((DictType?)null);

            var result = await ctrl.CreateAsync(new CreateDictItemRequest { DictTypeId = 99, Code = "m", Label = "Male" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DictTypeNotFound);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsBusinessException()
        {
            var (ctrl, itemMock, typeMock) = CreateController();
            typeMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictType, bool>>>(), default))
                .ReturnsAsync(new DictType { Id = 1, Code = "gender" });
            itemMock.Setup(s => s.IsCodeDuplicateAsync(1, "m")).ReturnsAsync(true);

            var act = async () => await ctrl.CreateAsync(new CreateDictItemRequest { DictTypeId = 1, Code = "m", Label = "Male" });

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.DuplicateDictItemCode);
        }

        [Fact]
        public async Task CreateAsync_Valid_ReturnsOk()
        {
            var (ctrl, itemMock, typeMock) = CreateController();
            typeMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictType, bool>>>(), default))
                .ReturnsAsync(new DictType { Id = 1, Code = "gender" });
            itemMock.Setup(s => s.IsCodeDuplicateAsync(1, "f")).ReturnsAsync(false);
            itemMock.Setup(s => s.AddAsync(It.IsAny<DictItem>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.CreateAsync(new CreateDictItemRequest { DictTypeId = 1, Code = "f", Label = "Female" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateAsync_ItemNotFound_ReturnsBadRequest()
        {
            var (ctrl, itemMock, _) = CreateController();
            itemMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictItem, bool>>>(), default))
                .ReturnsAsync((DictItem?)null);

            var result = await ctrl.UpdateAsync(99, new UpdateDictItemRequest { Label = "X" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DictItemNotFound);
        }

        [Fact]
        public async Task UpdateAsync_Found_ReturnsOk()
        {
            var (ctrl, itemMock, _) = CreateController();
            itemMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DictItem, bool>>>(), default))
                .ReturnsAsync(new DictItem { Id = 1, Code = "m", Label = "Male" });
            itemMock.Setup(s => s.UpdateAsync(It.IsAny<DictItem>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.UpdateAsync(1, new UpdateDictItemRequest { Label = "Male Updated" });

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
