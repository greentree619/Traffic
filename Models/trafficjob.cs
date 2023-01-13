using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Traffic.Models
{
    public class trafficjob
    {
    public int id { get; set; }

    [Required]
    [Display(Name = "Job Name")]
    public string job_name { get; set; }

    [Required]
    [Display(Name = "Journey Option")]
    public string journey_option { get; set; }

    [Display(Name = "Target URL")]
    public string target_url { get; set; }

    [Display(Name = "Target URL(Mobile)")]
    public string exttarget_url { get; set; }

    [Display(Name = "Name of the Business")]
    public string name_business { get; set; }

    [Required]
    [Display(Name = "Search Term")]
    public string search_term { get; set; }

    [Display(Name = "Search Engine")]
    [EnumDataType(typeof (trafficjob.SEARCH_ENGINE))]
    public trafficjob.SEARCH_ENGINE search_engine { get; set; }

    [Display(Name = "Agent Kind")]
    [EnumDataType(typeof (trafficjob.AGENT_KIND))]
    public trafficjob.AGENT_KIND agent_kind { get; set; }

    [Display(Name = "Agent of")]
    [EnumDataType(typeof (trafficjob.AGENT_AGE))]
    public trafficjob.AGENT_AGE agent_age { get; set; }

    [Display(Name = "Count")]
    public int session_count { get; set; }

    [Display(Name = "Wildcard")]
    public bool wild_card { get; set; }

    [Display(Name = "Impressions")]
    public bool impressions { get; set; }

    [Display(Name = "Cookie Website")]
    public bool cookie_website { get; set; }

    [Display(Name = "Proxy Setting")]
    public string proxy_setting { get; set; }

    [Display(Name = "Google Form URL")]
    public string journey_url { get; set; }

    [Display(Name = "Form Fields")]
    public string form_fields { get; set; }

    [Display(Name = "Time zone")]
    public string time_zone { get; set; }

    [Display(Name = "Start")]
    public string start_time { get; set; }

    [Display(Name = "End")]
    public string end_time { get; set; }

    public int group_id { get; set; }

    public string user_id { get; set; }

    [Display(Name = "Use local search")]
    public bool googlemaplocal { get; set; }

    public string googlemaplist { get; set; }

    [Required]
    public bool switch_on { get; set; }

    [Display(Name = "Snippet of content")]
    public string snippet_content { get; set; }

    [Display(Name = "Select Location")]
    public string location { get; set; }

    public string googlegeoresultselect { get; set; }

    public enum SEARCH_ENGINE
    {
      [Display(Name = "www.google.com")] EN,
      [Display(Name = "www.google.jp")] JP,
      [Display(Name = "www.google.co.kr")] KR,
      [Display(Name = "www.google.cn")] CN,
      [Display(Name = "www.google.hk")] HK,
      [Display(Name = "www.google.dk")] DK,
      [Display(Name = "www.google.co.uk")] UK,
      [Display(Name = "www.google.de")] DE,
      [Display(Name = "www.google.it")] IT,
      [Display(Name = "www.google.es")] ES,
      [Display(Name = "www.google.fr")] FR,
      [Display(Name = "www.google.nl")] NL,
      [Display(Name = "www.google.se")] SE,
      [Display(Name = "www.google.no")] NO,
      [Display(Name = "www.google.at")] AT,
      [Display(Name = "www.google.ch")] CH,
      [Display(Name = "www.google.ca")] CA,
      [Display(Name = "www.google.ie")] IE,
      [Display(Name = "www.google.co.za")] ZA,
      [Display(Name = "www.google.be")] BE,
      [Display(Name = "www.google.pl")] PL,
      [Display(Name = "www.google.bg")] BG,
      [Display(Name = "www.bing.com")] BI,
    }

    public enum AGENT_KIND
    {
      [Display(Name = "Both")] BOTH,
      [Display(Name = "Mobile")] MOBILE,
      [Display(Name = "Desktop")] DESKTOP,
    }

    public enum AGENT_AGE
    {
      [Display(Name = "All[New(90%)+Old(10%)]")] BOTH,
      [Display(Name = "New")] NEW,
      [Display(Name = "Old")] OLD,
    }
  }
}
