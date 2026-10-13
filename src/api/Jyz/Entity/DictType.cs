using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace Jyz.Entity
{
    public class DictType : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<DictItem> Items { get; set; } = new List<DictItem>();
    }
}
