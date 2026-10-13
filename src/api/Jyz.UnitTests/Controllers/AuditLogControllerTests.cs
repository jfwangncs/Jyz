using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.AuditLog;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class AuditLogControllerTests
    {
        [Fact]
        public async Task GetAllAsync_ReturnsOk()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);

            var svcMock = new Mock<IAuditLogService>();
            svcMock.Setup(s => s.GetPagedAsync(It.IsAny<QueryAuditLogRequest>()))
                .ReturnsAsync(new JfYu.Data.Model.PagedData<AuditLog>
                {
                    Data = [new AuditLog { Action = "Added", Resource = "Role" }],
                    TotalCount = 1,
                });

            var ctrl = new AuditLogController(svcMock.Object);

            var result = await ctrl.GetAllAsync(new QueryAuditLogRequest());

            var ok = result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;
            ok!.Code.Should().Be(ResponseCode.Success);
        }
    }
}
