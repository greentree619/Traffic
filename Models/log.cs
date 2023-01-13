using System;
using System.ComponentModel.DataAnnotations;

namespace Traffic.Models
{
  public class log
  {
    public int id { get; set; }

    [Required]
    public int job_id { get; set; }

    [Required]
    [Display(Name = "Execution Time")]
    public DateTime record_time { get; set; }

    public string search { get; set; }

    public string keyword { get; set; }

    public string agent { get; set; }

    public string proxy { get; set; }

    public string ip { get; set; }

    public string size { get; set; }

    public bool result { get; set; }

    public int failreason { get; set; }

    public int brandkind { get; set; }

    public int ranks { get; set; }

    public string worker { get; set; }
  }
}
