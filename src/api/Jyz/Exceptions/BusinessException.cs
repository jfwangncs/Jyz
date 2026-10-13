using System.Net;
using Jyz.Constants;

namespace Jyz.Exceptions
{
    public class BusinessException(ErrorCode errorCode, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : Exception()
    {
        public ErrorCode ErrorCode { get; } = errorCode;
        public HttpStatusCode StatusCode { get; } = statusCode;
    }
}
