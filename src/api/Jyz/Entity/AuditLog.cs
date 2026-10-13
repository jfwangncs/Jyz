using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace Jyz.Entity
{
    public class AuditLog : BaseEntity
    {
        public int? UserId { get; set; }

        [MaxLength(50)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Resource { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ResourceId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [MaxLength(50)]
        public string? IP { get; set; }
    }
}
