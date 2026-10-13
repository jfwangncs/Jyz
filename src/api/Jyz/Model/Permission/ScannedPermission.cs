using Jyz.Constants;

namespace Jyz.Model.Permission
{
    public class ScannedPermission
    {
        public required string Code { get; set; } 
        public PermissionType Type { get; set; } = PermissionType.Button;
        public string? ParentCode { get; set; } 
    }
}
