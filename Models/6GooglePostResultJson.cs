using System.Collections.Generic;

namespace Traffic.Models
{
  public class myBusinessPost
  {
    public string languageCode { get; set; }

    public string updateTime { get; set; }

    public string topicType { get; set; }

    public string name { get; set; }

    public List<myBusinessPostMedia> media { get; set; }

    public string searchUrl { get; set; }

    public string summary { get; set; }

    public string state { get; set; }

    public myBusinessCallToAction callToAction { get; set; }

    public string createTime { get; set; }

    public string views { get; set; }

    public string clicks { get; set; }
  }
}
