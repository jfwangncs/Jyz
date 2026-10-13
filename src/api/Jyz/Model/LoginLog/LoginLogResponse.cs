namespace Jyz.Model.LoginLog
{
    public class LoginLogResponse
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string? UserName { get; set; }

        public string? IP { get; set; }

        public string? UserAgent { get; set; }

        public int Result { get; set; }

        public string? FailReason { get; set; }

        public int? Platform { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
