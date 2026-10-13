using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Exceptions;
using Jyz.Model;
using Jyz.Model.Request;
using Jyz.Model.User;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class UserControllerTests
    {
        private static (UserController ctrl, Mock<IUserService> userMock,
            Mock<ICurrentUser> currentUserMock, Mock<IPermissionService> permMock)
            CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

            var userMock = new Mock<IUserService>();
            var permMock = new Mock<IPermissionService>();
            var currentUserMock = new Mock<ICurrentUser>();

            var ctrl = new UserController(userMock.Object, currentUserMock.Object);
            return (ctrl, userMock, currentUserMock, permMock);
        }

        private static BaseResponse<object>? GetOkValue(IActionResult result)
            => result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        private static BaseResponse<object>? GetBadValue(IActionResult result)
            => result.Should().BeOfType<BadRequestObjectResult>().Subject.Value as BaseResponse<object>;

        // ─── GetAllAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsOkWithPagedResult()
        {
            var (ctrl, userMock, _, _) = CreateController();
            var pagedData = new JfYu.Data.Model.PagedData<User>
            {
                Data = [new User { UserName = "alice" }],
                TotalCount = 1
            };
            userMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryRequest>())).ReturnsAsync(pagedData);

            var result = await ctrl.GetAllAsync(new QueryRequest());

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        // ─── GetByIdAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsOk()
        {
            var (ctrl, userMock, _, _) = CreateController();
            userMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(new User { Id = 1, UserName = "alice" });

            var result = await ctrl.GetByIdAsync(1);

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ReturnsBadRequest()
        {
            var (ctrl, userMock, _, _) = CreateController();
            userMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync((User?)null);

            var result = await ctrl.GetByIdAsync(999);

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        // ─── UpdateAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_Success_ReturnsOk()
        {
            var (ctrl, userMock, _, _) = CreateController();
            userMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUserRequest>(), default)).ReturnsAsync(true);

            var result = await ctrl.UpdateAsync(1, new UpdateUserRequest());

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateAsync_Failure_ReturnsBadRequest()
        {
            var (ctrl, userMock, _, _) = CreateController();
            userMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUserRequest>(), default)).ReturnsAsync(false);

            var result = await ctrl.UpdateAsync(1, new UpdateUserRequest());

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        [Fact]
        public async Task UpdateAsync_ServiceThrowsUserNotFound_PropagatesException()
        {
            var (ctrl, userMock, _, _) = CreateController();
            userMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateUserRequest>(), default))
                .ThrowsAsync(new BusinessException(ErrorCode.UserNotFound));

            var act = async () => await ctrl.UpdateAsync(999, new UpdateUserRequest());

            await act.Should().ThrowAsync<BusinessException>()
                .Where(e => e.ErrorCode == ErrorCode.UserNotFound);
        }

        // ─── GetUserInfo ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetUserInfo_UserFound_ReturnsOk()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(new User { Id = 1, UserName = "alice" });

            var result = await ctrl.GetUserInfo();

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetUserInfo_UserNotFound_ReturnsBadRequest()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.GetOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync((User?)null);

            var result = await ctrl.GetUserInfo();

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        // ─── ChangePasswordAsync ──────────────────────────────────────────────

        [Fact]
        public async Task ChangePasswordAsync_Success_ReturnsOk()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequest>(), default)).ReturnsAsync(true);

            var result = await ctrl.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangePasswordAsync_Failure_ReturnsBadRequest()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequest>(), default)).ReturnsAsync(false);

            var result = await ctrl.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "o", NewPassword = "n" });

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        // ─── UpdateProfileAsync ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateProfileAsync_Success_ReturnsOk()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.UpdateProfileAsync(1, It.IsAny<UpdateProfileRequest>(), default)).ReturnsAsync(true);

            var result = await ctrl.UpdateProfileAsync(new UpdateProfileRequest());

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateProfileAsync_Failure_ReturnsBadRequest()
        {
            var (ctrl, userMock, currentUserMock, _) = CreateController();
            currentUserMock.Setup(c => c.Id).Returns(1);
            userMock.Setup(s => s.UpdateProfileAsync(1, It.IsAny<UpdateProfileRequest>(), default)).ReturnsAsync(false);

            var result = await ctrl.UpdateProfileAsync(new UpdateProfileRequest());

            GetBadValue(result)!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }
    }
}
