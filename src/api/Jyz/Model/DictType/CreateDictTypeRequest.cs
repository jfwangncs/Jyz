namespace Jyz.Model.DictType
{
    public class CreateDictTypeRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
