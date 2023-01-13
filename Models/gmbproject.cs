using System.ComponentModel.DataAnnotations;

namespace Traffic.Models
{
  public class gmbproject
  {
    public int id { get; set; }

    public int group_id { get; set; }

    public string user_id { get; set; }

    [Required]
    [Display(Name = "Project Name")]
    public string project_name { get; set; }

    public string refresh_token { get; set; }

    public int location_count { get; set; }

    public int post_count { get; set; }

    public int folder_id { get; set; }
  }
}
