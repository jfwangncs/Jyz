namespace Jyz.Model.DictItem
{
    public class CreateDictItemRequest
    {
        public required int DictTypeId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public int? Sort { get; set; }
    }
}
