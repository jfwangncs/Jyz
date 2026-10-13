using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.Role;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class RoleControllerTests
    {
        private static (RoleController ctrl, Mock<IRoleService> roleMock) CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

            var roleMock = new Mock<IRoleService>();
            var ctrl = new RoleController(roleMock.Object);
            return (ctrl, roleMock);
        }

        private static BaseResponse<object>? GetOkValue(IActionResult result)
            => result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        private static BaseResponse<object>? GetBadValue(IActionResult result)
            => result.Should().BeOfType<BadRequestObjectResult>().Subject.Value as BaseResponse<object>;

        // ─── GetAllAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsOk()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<Role> { Data = [], TotalCount = 0 });

            var result = await ctrl.GetAllAsync(new QueryRequest());

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        // ─── GetByIdAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_RoleExists_ReturnsOk()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(new Role { Id = 1, Name = "Admin" });

            var result = await ctrl.GetByIdAsync(1);

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync((Role?)null);

            var result = await ctrl.GetByIdAsync(999);

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.RoleNotFound);
        }

        // ─── CreateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NewRole_ReturnsOkWithRole()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync((Role?)null);
            roleMock.Setup(s => s.AddAsync(It.IsAny<Role>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.CreateAsync(new CreateRoleRequest { Name = "Editor" });

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(new Role { Id = 1, Name = "Admin" });

            var result = await ctrl.CreateAsync(new CreateRoleRequest { Name = "Admin" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DuplicateRole);
        }

        // ─── UpdateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_Success_ReturnsOk()
        {
            var (ctrl, roleMock) = CreateController();
            var role = new Role { Id = 1, Name = "Admin" };
            roleMock.SetupSequence(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(role)       // first: role exists
                .ReturnsAsync((Role?)null); // second: no duplicate name
            roleMock.Setup(s => s.UpdateAsync(It.IsAny<Role>(), default)).Returns(Task.FromResult(1));

            var result = await ctrl.UpdateAsync(1, new UpdateRoleRequest { Name = "Super Admin" });

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task UpdateAsync_RoleNotFound_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync((Role?)null);

            var result = await ctrl.UpdateAsync(999, new UpdateRoleRequest { Name = "X" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.RoleNotFound);
        }

        [Fact]
        public async Task UpdateAsync_DuplicateName_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.SetupSequence(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(new Role { Id = 1, Name = "Admin" })  // role exists
                .ReturnsAsync(new Role { Id = 2, Name = "Editor" }); // name collision

            var result = await ctrl.UpdateAsync(1, new UpdateRoleRequest { Name = "Editor" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.DuplicateRole);
        }

        [Fact]
        public async Task UpdateAsync_SaveFails_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            var role = new Role { Id = 1, Name = "Admin" };
            roleMock.SetupSequence(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(role)
                .ReturnsAsync((Role?)null);
            roleMock.Setup(s => s.UpdateAsync(It.IsAny<Role>(), default)).ReturnsAsync(0);

            var result = await ctrl.UpdateAsync(1, new UpdateRoleRequest { Name = "New" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        // ─── AssignPermissionsAsync ───────────────────────────────────────────

        [Fact]
        public async Task AssignPermissionsAsync_RoleExists_ReturnsOk()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync(new Role { Id = 1, Name = "Admin" });

            var result = await ctrl.AssignPermissionsAsync(1, new AssignPermissionsRequest { PermissionIds = [1, 2] });

            result.Should().BeOfType<OkObjectResult>();
            roleMock.Verify(s => s.AssignPermissionsAsync(1, It.Is<List<int>>(ids => ids.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task AssignPermissionsAsync_RoleNotFound_ReturnsBadRequest()
        {
            var (ctrl, roleMock) = CreateController();
            roleMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), default))
                .ReturnsAsync((Role?)null);

            var result = await ctrl.AssignPermissionsAsync(999, new AssignPermissionsRequest { PermissionIds = [1] });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.RoleNotFound);
        }
    }
}
