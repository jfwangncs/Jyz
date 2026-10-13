using System.Net;
using System.Net.Http.Json;
using Jyz.Constants;
using Jyz.Exceptions;
using Jyz.Extensions;
using Jyz.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jyz.UnitTests.Extensions
{
    public class UseCustomExceptionHandlerTests
    {
        private static async Task<(HttpResponseMessage response, WebApplication host)> SendRequestAsync(string path)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = Path.GetTempPath()
            });
            builder.WebHost.UseTestServer();
            builder.Services.AddRouting();
            builder.Services.AddControllers().AddJsonOptions(_ => { });
            var app = builder.Build();
            app.UseRouting();
            app.UseCustomExceptionHandler();
            app.Map("/business-error", _ => throw new BusinessException(ErrorCode.UserNotFound));
            app.Map("/unhandled-error", _ => throw new InvalidOperationException("boom"));

            await app.StartAsync();
            var client = app.GetTestClient();
            var response = await client.GetAsync(path);
            return (response, app);
        }

        [Fact]
        public async Task BusinessException_ReturnsMappedStatusAndErrorCode()
        {
            var (response, host) = await SendRequestAsync("/business-error");
            await using var _ = host;

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            body!.Code.Should().Be(ResponseCode.Failed);
            body.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }

        [Fact]
        public async Task UnhandledException_ReturnsInternalServerErrorWithSystemError()
        {
            var (response, host) = await SendRequestAsync("/unhandled-error");
            await using var _ = host;

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            body!.Code.Should().Be(ResponseCode.Error);
            body.ErrorCode.Should().Be(ErrorCode.SystemError);
        }
    }
}
