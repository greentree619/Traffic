using System.Collections.Generic;

namespace Traffic.Models
{
  public class myBusinessPostInsight
  {
    public string timeZone { get; set; }

    public List<myBusinesslocalPostMetrics> localPostMetrics { get; set; }

    public string name { get; set; }
  }
}
