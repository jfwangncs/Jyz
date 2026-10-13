using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Model;
using Jyz.Model.RedisCache;
using Jyz.Services.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class RedisCacheControllerTests
    {
        private static (RedisCacheController ctrl, Mock<IRedisCacheService> svcMock) CreateController()
        {
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            var svcMock = new Mock<IRedisCacheService>();
            var ctrl = new RedisCacheController(svcMock.Object);
            return (ctrl, svcMock);
        }

        private static BaseResponse<object>? GetOkValue(IActionResult result)
            => result.Should().BeOfType<OkObjectResult>().Subject.Value as BaseResponse<object>;

        [Fact]
        public async Task GetAllAsync_ReturnsOkWithList()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetAllAsync()).ReturnsAsync(
            [
                new RedisCacheItemResponse { Key = "key1", Value = "val1" },
                new RedisCacheItemResponse { Key = "key2", Value = "val2" }
            ]);

            var result = await ctrl.GetAllAsync();

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task GetAllAsync_EmptyList_ReturnsOkWithEmptyList()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

            var result = await ctrl.GetAllAsync();

            GetOkValue(result)!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public async Task DeleteAsync_EmptyKeys_ReturnsOkWithoutCallingService()
        {
            var (ctrl, svcMock) = CreateController();

            var result = await ctrl.DeleteAsync(new DeleteRedisCacheRequest { Keys = [] });

            result.Should().BeOfType<OkObjectResult>();
            svcMock.Verify(s => s.DeleteAsync(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithKeys_CallsServiceAndReturnsOk()
        {
            var (ctrl, svcMock) = CreateController();
            svcMock.Setup(s => s.DeleteAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);

            var result = await ctrl.DeleteAsync(new DeleteRedisCacheRequest { Keys = ["key1", "key2"] });

            result.Should().BeOfType<OkObjectResult>();
            svcMock.Verify(s => s.DeleteAsync(It.Is<List<string>>(k => k.Count == 2)), Times.Once);
        }
    }
}
