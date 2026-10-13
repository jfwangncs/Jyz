using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.LoginLog;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class LoginLogControllerTests
    {
        [Fact]
        public async Task GetAllAsync_ReturnsOk()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

            var svcMock = new Mock<ILoginLogService>();
            svcMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryLoginLogRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<LoginLog>
                {
                    Data = [new LoginLog { UserName = "u" }],
                    TotalCount = 1,
                });

            var ctrl = new LoginLogController(svcMock.Object);

            var result = await ctrl.GetAllAsync(new QueryLoginLogRequest());

            var ok = result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;
            ok!.Code.Should().Be(ResponseCode.Success);
        }
    }
}
