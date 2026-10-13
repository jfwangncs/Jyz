namespace Jyz.Model.Role
{
    public class CreateRoleRequest
    {
        public required string Name { get; set; }

        public string? Description { get; set; }
    }
}
