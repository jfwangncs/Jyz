namespace Jyz.Model.AuditLog
{
    public class QueryAuditLogRequest : QueryRequest
    {
        public string? Resource { get; set; }

        public string? Action { get; set; }

        public int? UserId { get; set; }
    }
}
