using Jyz.Model.DictItem;

namespace Jyz.Model.DictType
{
    public class DictTypeResponse
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Status { get; set; }

        public DateTime CreatedTime { get; set; }

        public List<DictItemResponse> Items { get; set; } = new();
    }
}
