using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Traffic.Data;
using Traffic.Models;

namespace Traffic.Controllers
{
  [Route("api/[controller]")]
  public class JourneyController : Controller
  {
    private IHttpContextAccessor _accessor;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public JourneyController(
      UserManager<ApplicationUser> userManager,
      ApplicationDbContext context,
      IHttpContextAccessor accessor)
    {
      this._userManager = userManager;
      this._context = context;
      this._accessor = accessor;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string UserId)
    {
      string ip = this._accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
      SettingManager.Logger("Journey Fetching request from : " + UserId);
      Journey result = new Journey();
      Random rnd = new Random();
      if (await this._userManager.FindByIdAsync(UserId) == null)
      {
        SettingManager.Logger("Journey Fetching request [Incorrect Req]");
        OkObjectResult okObjectResult = new OkObjectResult((object) "Incorrect Request");
      }
      Startup.ResetRunningFlagJourneiesForUser(this._context, ip);
      trafficjob job = Startup.PickNextJourney(this._context, ip);
      if (job != null)
      {
        SettingManager.Logger("Journey Fetching result | jobid: " + job.id.ToString());
        DisplayAttribute customAttribute = CustomAttributeExtensions.GetCustomAttribute<DisplayAttribute>((MemberInfo) job.search_engine.GetType().GetField(job.search_engine.ToString()));
        string agetn_desc = string.Empty;
        string str = BrowserAgent.SpinAgent((int) job.agent_kind, (int) job.agent_age, out agetn_desc);
        result.Id = job.id;
        result.Startpage = "https://" + customAttribute.Name;
        result.keyword = BrowserAgent.SpinSearchTerm(job.search_term, rnd.Next(0, 100));
        result.target = job.target_url;
        result.namebusiness = job.name_business;
        result.job_name = job.job_name;
        result.SelJourney = job.journey_option;
        result.agent = str;
        result._proxy = Startup.selectproxy(job);
        result.wholesearchterm = job.search_term;
        result.agent_desc = agetn_desc;
        result.exttarget = job.exttarget_url;
        result.wildcard = job.wild_card;
        result.impressions = job.impressions;
        result.cookie_website = job.cookie_website;
        if (result.SelJourney.IndexOf("T1") >= 0)
        {
          string retbusiness;
          result.keyword = BrowserAgent.SpinT1SearchTermBusiness(job.search_term, job.name_business, out retbusiness, rnd.Next(0, 100));
          result.namebusiness = retbusiness;
        }
      }
      else
        SettingManager.Logger("Journey Fetching result | no journey");
      IActionResult actionResult = (IActionResult) new OkObjectResult((object) result);
      ip = (string) null;
      result = (Journey) null;
      rnd = (Random) null;
      return actionResult;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] string UserId, [FromForm] string Result)
    {
      JourneyController journeyController = this;
      SettingManager.Logger("Journey Reporting request from : " + UserId + " Result " + Result);
      if (await journeyController._userManager.FindByIdAsync(UserId) == null)
      {
        SettingManager.Logger("Journey Reporting request [Incorrect req]");
        return (IActionResult) journeyController.NotFound();
      }
      try
      {
        string str = journeyController._accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        log result = JsonConvert.DeserializeObject<log>(Result);
        result.worker = str;
        Startup.UpdateJourneyResult(journeyController._context, result);
      }
      catch (Exception ex)
      {
        SettingManager.Logger("Journey Reporting request Exception " + ex?.ToString());
      }
      return (IActionResult) new OkObjectResult((object) "Thanks");
    }
  }
}
