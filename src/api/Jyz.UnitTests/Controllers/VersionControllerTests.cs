using Jyz.Model;
using Microsoft.AspNetCore.Mvc;
using V2VersionController = Jyz.Controllers.V2.VersionController;
using VersionController = Jyz.Controllers.VersionController;

namespace Jyz.UnitTests.Controllers
{
    public class VersionControllerTests
    {
        [Fact]
        public void Get_ReturnsOkWithVersionInfo()
        {
            var ctrl = new VersionController();

            var result = ctrl.Get();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var resp = ok.Value.Should().BeOfType<BaseResponse<object>>().Subject;
            var value = resp.Data.Should().NotBeNull().And.Subject;
            var nameProp = value.GetType().GetProperty("name");
            var environmentProp = value.GetType().GetProperty("environment");

            nameProp.Should().NotBeNull();
            ((string?)nameProp.GetValue(value)).Should().NotBeNullOrEmpty();
            environmentProp.Should().NotBeNull();
            ((string?)environmentProp.GetValue(value)).Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void V2_Get_ReturnsOkWithApiVersion2()
        {
            var ctrl = new V2VersionController();

            var result = ctrl.Get();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var resp = ok.Value.Should().BeOfType<BaseResponse<object>>().Subject;
            var value = resp.Data.Should().NotBeNull().And.Subject;
            var apiVersionProp = value.GetType().GetProperty("apiVersion");

            apiVersionProp.Should().NotBeNull();
            apiVersionProp.GetValue(value).Should().Be(2.0);
        }
    }
}
