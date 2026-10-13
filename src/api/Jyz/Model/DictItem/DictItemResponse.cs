namespace Jyz.Model.DictItem
{
    public class DictItemResponse
    {
        public int Id { get; set; }

        public int DictTypeId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public int Sort { get; set; }

        public int Status { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
