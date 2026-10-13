using Jyz.Extensions;
using Jyz.Constants;

namespace Jyz.Model
{
    public class BaseResponse<T>
    {
        public ResponseCode Code { get; set; } = ResponseCode.Success;
        public string Message { get; set; } = ResponseCode.Success.GetDescription();
        public ErrorCode? ErrorCode { get; set; }
        public T? Data { get; set; }
    }
}
