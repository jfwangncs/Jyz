using Jyz.Constants;

namespace Jyz.Model.Request
{
    public class UpdateUserRequest
    {
        public string? NickName { get; set; }

        public string? RealName { get; set; }

        public string? Phone { get; set; }

        public string? Avatar { get; set; }

        public Gender? Gender { get; set; }

        public string? Province { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public int? Status { get; set; }

        public List<int>? RoleIds { get; set; }
    }
}
