namespace Jyz.Model.LoginLog
{
    public class QueryLoginLogRequest : QueryRequest
    {
        public int? Result { get; set; }

        public int? Platform { get; set; }
    }
}
