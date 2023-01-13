using System;
using System.ComponentModel.DataAnnotations;

namespace Traffic.Models
{
  public class dailystate
  {
    public int id { get; set; }

    [Required]
    public int job_id { get; set; }

    public int state { get; set; }

    public DateTime predict_time { get; set; }

    public DateTime start_time { get; set; }

    public DateTime end_time { get; set; }

    [Required]
    public bool isrunning { get; set; }

    public string runner_id { get; set; }

    public int consfailcount { get; set; }

    public bool isoldjourney { get; set; }
  }
}
