using Jyz.Constants;

namespace Jyz.Model.Permission
{
    public class PermissionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public PermissionType Type { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int Sort { get; set; }
        public int? ParentId { get; set; }
        public int Status { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
