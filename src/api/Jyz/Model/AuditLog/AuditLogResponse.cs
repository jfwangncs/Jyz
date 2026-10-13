namespace Jyz.Model.AuditLog
{
    public class AuditLogResponse
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string? UserName { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Resource { get; set; } = string.Empty;

        public string? ResourceId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? IP { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
