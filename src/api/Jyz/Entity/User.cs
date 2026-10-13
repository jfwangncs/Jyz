using System.ComponentModel.DataAnnotations;
using JfYu.Data.Model;
using Jyz.Constants;

namespace Jyz.Entity
{
    public class User : BaseEntity
    {
        [MaxLength(100)]
        public string? SessionKey { get; set; }

        [MaxLength(100)]
        public string? OpenId { get; set; }

        [MaxLength(100)]
        public string? UnionId { get; set; }

        [MaxLength(50)]
        public string? NickName { get; set; }

        [MaxLength(50)]
        [Required]
        public required string UserName { get; set; }

        [MaxLength(50)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Password { get; set; }

        [MaxLength(50)]
        public string? RealName { get; set; }

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(10)]
        public Gender? Gender { get; set; }

        [MaxLength(50)]
        public string? Province { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public DateTime LastLoginTime { get; set; } = DateTime.Now;

        public ICollection<Role> Roles { get; set; } = [];

    }
}
