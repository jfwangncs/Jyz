using Jyz.Model.Permission;

namespace Jyz.Model.Response
{
    public class RoleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
        public ICollection<PermissionResponse> Permissions { get; set; } = [];
    }
}
