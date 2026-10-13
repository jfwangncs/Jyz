using Jyz.Constants;

namespace Jyz.Model.Permission
{
    public class UpdatePermissionRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public PermissionType? Type { get; set; }
        public int? Sort { get; set; }
        public int? Status { get; set; }
    }
}
