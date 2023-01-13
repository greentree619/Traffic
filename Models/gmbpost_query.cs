using System;
using System.ComponentModel.DataAnnotations;

namespace Traffic.Models
{
  public class gmbpost_query
  {
    public int id { get; set; }

    public string refreshtoken { get; set; }

    public string locationname { get; set; }

    [Required]
    public DateTime time { get; set; }

    public string languagecode { get; set; }

    public string summary { get; set; }

    public string actiontype { get; set; }

    public string actionurl { get; set; }

    public string mediaformat { get; set; }

    public string mediaurl { get; set; }
  }
}
