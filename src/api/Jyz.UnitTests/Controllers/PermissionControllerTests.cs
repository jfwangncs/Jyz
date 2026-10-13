using System.Linq.Expressions;
using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Model.Permission;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Jyz.Model;

namespace Jyz.UnitTests.Controllers
{
    public class PermissionControllerTests
    {
        private static (PermissionController ctrl, Mock<IPermissionService> permMock) CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

            var permMock = new Mock<IPermissionService>();
            var ctrl = new PermissionController(permMock.Object);
            return (ctrl, permMock);
        }

        private static BaseResponse<object>? GetOk(IActionResult r)
            => r.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        private static BaseResponse<object>? GetBad(IActionResult r)
            => r.Should().BeOfType<BadRequestObjectResult>().Subject.Value as BaseResponse<object>;

        [Fact]
        public async Task GetListAsync_ReturnsOk()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetListAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync([new Permission { Id = 1, Code = "c1", Name = "n1" }]);

            var result = await ctrl.GetListAsync();

            GetOk(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsOk()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync(new Permission { Id = 1, Code = "c1", Name = "n1" });

            var result = await ctrl.GetByIdAsync(1);

            GetOk(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsBad()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync((Permission?)null);

            var result = await ctrl.GetByIdAsync(99);

            GetBad(result)!.ErrorCode.Should().Be(ErrorCode.PermissionNotFound);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ReturnsBad()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync(new Permission { Code = "dup" });

            var result = await ctrl.CreateAsync(new CreatePermissionRequest { Code = "dup", Name = "n", Type = PermissionType.Button });

            GetBad(result)!.ErrorCode.Should().Be(ErrorCode.DuplicatePermission);
        }

        [Fact]
        public async Task CreateAsync_Success_ReturnsOk()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync((Permission?)null);
            permMock.Setup(s => s.AddAsync(It.IsAny<Permission>(), default)).ReturnsAsync(1);

            var result = await ctrl.CreateAsync(new CreatePermissionRequest { Code = "new", Name = "n", Type = PermissionType.Button });

            GetOk(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task CreateAsync_AddFails_ReturnsBad()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync((Permission?)null);
            permMock.Setup(s => s.AddAsync(It.IsAny<Permission>(), default)).ReturnsAsync(0);

            var result = await ctrl.CreateAsync(new CreatePermissionRequest { Code = "new", Name = "n", Type = PermissionType.Button });

            GetBad(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsBad()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync((Permission?)null);

            var result = await ctrl.UpdateAsync(99, new UpdatePermissionRequest { Name = "x" });

            GetBad(result)!.ErrorCode.Should().Be(ErrorCode.PermissionNotFound);
        }

        [Fact]
        public async Task UpdateAsync_Success_ReturnsOk()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync(new Permission { Id = 1, Code = "c", Name = "old" });
            permMock.Setup(s => s.UpdateAsync(It.IsAny<Permission>(), default)).ReturnsAsync(1);

            var result = await ctrl.UpdateAsync(1, new UpdatePermissionRequest { Name = "new" });

            GetOk(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task UpdateAsync_UpdateFails_ReturnsBad()
        {
            var (ctrl, permMock) = CreateController();
            permMock.Setup(s => s.GetOneAsync(It.IsAny<Expression<Func<Permission, bool>>>(), default))
                .ReturnsAsync(new Permission { Id = 1, Code = "c", Name = "old" });
            permMock.Setup(s => s.UpdateAsync(It.IsAny<Permission>(), default)).ReturnsAsync(0);

            var result = await ctrl.UpdateAsync(1, new UpdatePermissionRequest { Name = "new" });

            GetBad(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        [Fact]
        public void Sync_InvokesService_ReturnsOk()
        {
            var (ctrl, permMock) = CreateController();

            var result = ctrl.Sync();

            result.Should().BeOfType<OkObjectResult>();
            permMock.Verify(s => s.SyncAsync(), Times.Once);
        }
    }
}
