namespace Jyz.Model
{
  public class QueryRequest
  {
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string SearchKey { get; set; } = "";

    public int? Status { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
  }
}
