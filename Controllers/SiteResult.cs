using System.Collections.Generic;

namespace Traffic.Controllers
{
  public class SiteResult
  {
    public List<Row> rows { get; set; }

    public string responseAggregationType { get; set; }
  }
}
