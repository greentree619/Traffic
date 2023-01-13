using System.ComponentModel.DataAnnotations;

namespace Traffic.Models
{
  public class location
  {
    public int id { get; set; }

    public int depth { get; set; }

    public int parentid { get; set; }

    [Required]
    public string label { get; set; }

    public string shortlabel { get; set; }
  }
}
