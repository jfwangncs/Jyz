using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace Jyz.Entity
{
    public class LoginLog : BaseEntity
    {
        public int? UserId { get; set; }

        [MaxLength(50)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? IP { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int Result { get; set; }

        [MaxLength(200)]
        public string? FailReason { get; set; }

        public int? Platform { get; set; }
    }
}
