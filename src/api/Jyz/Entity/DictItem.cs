using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace Jyz.Entity
{
    public class DictItem : BaseEntity
    {
        public int DictTypeId { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        public int Sort { get; set; }

        public DictType? DictType { get; set; }
    }
}
