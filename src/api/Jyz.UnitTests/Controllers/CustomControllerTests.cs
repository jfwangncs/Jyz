using System.Net;
using Jyz.Constants;
using Jyz.Controllers;
using Jyz.Model;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.UnitTests.Controllers
{
    public class CustomControllerTests
    {
        // Probe subclass to expose protected helpers.
        private sealed class Probe : CustomController
        {
            public new OkObjectResult Ok() => base.Ok();
            public new OkObjectResult Ok<T>(T data) => base.Ok(data);
            public new OkObjectResult Ok<T>(string message, T data) => base.Ok(message, data);
            public new BadRequestObjectResult BadRequest(ResponseCode c, ErrorCode? e) => base.BadRequest(c, e);
            public new BadRequestObjectResult BadRequest(ErrorCode e) => base.BadRequest(e);
            public new BadRequestObjectResult BadRequest<T>(ResponseCode c, ErrorCode? e, T data) => base.BadRequest(c, e, data);
            public new BadRequestObjectResult BadRequest<T>(ErrorCode e, T data) => base.BadRequest(e, data);
            public new ObjectResult Result<T>(ResponseCode c, ErrorCode e, T data, HttpStatusCode http) => base.Result(c, e, data, http);
        }

        private static BaseResponse<string>? AsStringBody(IActionResult r)
            => ((ObjectResult)r).Value as BaseResponse<string>;

        private static BaseResponse<object>? AsObjectBody(IActionResult r)
            => ((ObjectResult)r).Value as BaseResponse<object>;

        [Fact]
        public void Ok_NoArgs_ReturnsSuccess()
        {
            var body = AsStringBody(new Probe().Ok());
            body!.Code.Should().Be(ResponseCode.Success);
        }

        [Fact]
        public void Ok_WithData_WrapsData()
        {
            var body = AsObjectBody(new Probe().Ok(42));
            body!.Code.Should().Be(ResponseCode.Success);
            body.Data.Should().Be(42);
        }

        [Fact]
        public void Ok_WithMessageAndData_UsesProvidedMessage()
        {
            var body = AsObjectBody(new Probe().Ok("hi", "x"));
            body!.Message.Should().Be("hi");
            body.Data.Should().Be("x");
        }

        [Fact]
        public void BadRequest_ResponseCodeAndErrorCode_PopulatesBoth()
        {
            var body = AsObjectBody(new Probe().BadRequest(ResponseCode.Failed, ErrorCode.OperationFailed));
            body!.Code.Should().Be(ResponseCode.Failed);
            body.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        [Fact]
        public void BadRequest_ErrorCodeOnly_DefaultsToFailed()
        {
            var body = AsObjectBody(new Probe().BadRequest(ErrorCode.OperationFailed));
            body!.Code.Should().Be(ResponseCode.Failed);
            body.ErrorCode.Should().Be(ErrorCode.OperationFailed);
        }

        [Fact]
        public void BadRequest_ResponseCodeErrorCodeData_IncludesData()
        {
            var body = AsObjectBody(new Probe().BadRequest(ResponseCode.Failed, ErrorCode.OperationFailed, "data"));
            body!.Data.Should().Be("data");
        }

        [Fact]
        public void BadRequest_ErrorCodeAndData_IncludesData()
        {
            var body = AsObjectBody(new Probe().BadRequest(ErrorCode.OperationFailed, "data"));
            body!.ErrorCode.Should().Be(ErrorCode.OperationFailed);
            body.Data.Should().Be("data");
        }

        [Fact]
        public void Result_SetsStatusCode()
        {
            var result = new Probe().Result(ResponseCode.Failed, ErrorCode.UnauthorizedError, "x", HttpStatusCode.Unauthorized);

            result.StatusCode.Should().Be(401);
            (result.Value as BaseResponse<object>)!.ErrorCode.Should().Be(ErrorCode.UnauthorizedError);
        }
    }
}
