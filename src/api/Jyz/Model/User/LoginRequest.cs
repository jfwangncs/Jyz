using Jyz.Constants;

namespace Jyz.Model.User
{
    public class LoginRequest
    {
        public string? Phone { get; set; }

        public string? Code { get; set; }


        public string? UserName { get; set; }

        public string? Password { get; set; }

        public Platform Platform { get; set; } = Platform.Web;
    }
}
