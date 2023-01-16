using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Traffic.Data;
using Traffic.Models;

namespace Traffic.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private string[] countrylist;
        private static IWebDriver driver;

        public string GetCountryCode(string country)
        {
            string countryCode = "";
            if (this.countrylist == null)
            {
                string path = Directory.GetCurrentDirectory() + "/country.csv";
                if (System.IO.File.Exists(path))
                    this.countrylist = System.IO.File.ReadAllLines(path);
            }
            if (this.countrylist != null)
            {
                foreach (string str in this.countrylist)
                {
                    string[] strArray = str.Split(",", StringSplitOptions.None);
                    if (strArray.Length == 2 && strArray[1].Trim() == country.Trim())
                    {
                        countryCode = strArray[0];
                        break;
                    }
                }
            }
            return countryCode;
        }

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            this._userManager = userManager;
            this._context = context;
        }

        public async Task<IActionResult> JobIndex(string id)
        {
            HomeController homeController = this;
            List<trafficjob> resultJobs = new List<trafficjob>();
            IEnumerable<trafficjob> topJobs = (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(homeController._context.trafficjob.Where<trafficjob>((Expression<Func<trafficjob, bool>>)(w => w.group_id == -1)), new CancellationToken());
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(currentUserID);
            bool flag1 = homeController.User.IsInRole("Admin");
            foreach (trafficjob trafficjob in topJobs)
            {
                if (string.IsNullOrEmpty(id))
                {
                    if (trafficjob.user_id != currentUserID)
                        continue;
                }
                else if (trafficjob.user_id != id)
                    continue;
                resultJobs.Add(trafficjob);
            }
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            if (flag1)
            {
                flag2 = true;
                flag3 = true;
                flag4 = true;
            }
            else
            {
                if (byIdAsync.EnableGMB)
                    flag2 = true;
                if (byIdAsync.EnableHtml)
                    flag3 = true;
                if (byIdAsync.EnableTwitter)
                    flag4 = true;
            }
            homeController.ViewData["EnableGMB"] = (object)flag2;
            homeController.ViewData["EnableHtml"] = (object)flag3;
            homeController.ViewData["EnableTwitter"] = (object)flag4;
            IActionResult actionResult = (IActionResult)homeController.View((object)resultJobs);
            resultJobs = (List<trafficjob>)null;
            topJobs = (IEnumerable<trafficjob>)null;
            currentUserID = (string)null;
            return actionResult;
        }

        public async Task<IActionResult> Index()
        {
            HomeController homeController = this;
            List<trafficjob> trafficjobList = new List<trafficjob>();
            IEnumerable<trafficjob> listAsync = (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(homeController._context.trafficjob.Where<trafficjob>((Expression<Func<trafficjob, bool>>)(w => w.group_id == -1)), new CancellationToken());
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            bool flag = homeController.User.IsInRole("Admin");
            List<ApplicationUser> list = homeController._userManager.Users.ToList<ApplicationUser>();
            return flag ? (IActionResult)homeController.View((object)list) : (IActionResult)homeController.RedirectToAction("JobIndex", (object)new
            {
                id = str
            });
        }

        public async Task<IActionResult> Jobs(int id, string userid)
        {
            HomeController homeController = this;
            List<trafficjob> resultJobs = new List<trafficjob>();
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            bool IsAdmin = homeController.User.IsInRole("Admin");
            int nCreatedCount = 0;
            DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
            Expression<Func<trafficjob, bool>> predicate1 = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
            foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate1), new CancellationToken()))
            {
                if (trafficjob2.user_id == currentUserID)
                    ++nCreatedCount;
            }

            this.ViewBag.CanCreateJob = true;
            if (!IsAdmin && nCreatedCount >= user.MaxJourneyCount)
            {
                this.ViewBag.CanCreateJob = false;
            }

            DbSet<trafficjob> trafficjob3 = homeController._context.trafficjob;
            Expression<Func<trafficjob, bool>> predicate2 = (Expression<Func<trafficjob, bool>>) (w => w.group_id == id);
            foreach (trafficjob trafficjob4 in (IEnumerable<trafficjob>) await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob3.Where<trafficjob>(predicate2), new CancellationToken()))
              resultJobs.Add(trafficjob4);

            this.ViewBag.FolderId = id;
            this.ViewBag.UserId = userid;
            this.ViewBag.EnableCreateJobFromFile = user.EnableCreateJobFromFile;
            this.ViewBag.EnableGoogleSearchConsole = user.EnableGoogleSearchConsole;

            IActionResult actionResult = (IActionResult)homeController.View((object)resultJobs);
            resultJobs = (List<trafficjob>)null;
            currentUserID = (string)null;
            user = (ApplicationUser)null;
            return actionResult;
        }

        public async Task<IActionResult> Details(int id)
        {
            HomeController homeController = this;
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            trafficjob model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            return model != null ? (IActionResult)homeController.View((object)model) : (IActionResult)homeController.NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> BulkResetIgnore(string param)
        {
            HomeController homeController = this;
            if (!string.IsNullOrEmpty(param.Trim()))
            {
                string[] ids = param.Trim().Split(',', StringSplitOptions.None);
                for (int i = 0; i < ((IEnumerable<string>)ids).Count<string>(); ++i)
                {
                    if (!string.IsNullOrEmpty(ids[i].Trim()))
                    {
                        int id = 0;
                        if (int.TryParse(ids[i], out id) && id != 0)
                        {
                            dailystate entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<dailystate>((IQueryable<dailystate>)homeController._context.dailystate, (Expression<Func<dailystate, bool>>)(m => m.id == id), new CancellationToken());
                            homeController._context.Entry<dailystate>(entity).Reload();
                            if (entity.state == 3)
                            {
                                entity.state = 0;
                                homeController._context.dailystate.Update(entity);
                            }
                        }
                    }
                }
                homeController._context.SaveChanges();
                ids = (string[])null;
            }
            return (IActionResult)homeController.RedirectToAction("Statistics");
        }

        [HttpPost]
        public async Task<IActionResult> BulkStart(string param)
        {
            HomeController homeController = this;
            int num = -1;
            if (!string.IsNullOrEmpty(param.Trim()))
            {
                string[] ids = param.Trim().Split(',', StringSplitOptions.None);
                for (int i = 0; i < ((IEnumerable<string>)ids).Count<string>(); ++i)
                {
                    if (!string.IsNullOrEmpty(ids[i].Trim()))
                    {
                        int id = 0;
                        if (int.TryParse(ids[i], out id) && id != 0)
                        {
                            trafficjob entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
                            num = entity.group_id;
                            entity.switch_on = true;
                            Startup.AddScheduleForToday(homeController._context, entity.id);
                            homeController._context.trafficjob.Update(entity);
                        }
                    }
                }
                homeController._context.SaveChanges();
                ids = (string[])null;
            }
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = num
            });
        }

        [HttpPost]
        public async Task<IActionResult> BulkStop(string param)
        {
            HomeController homeController = this;
            int num = -1;
            if (!string.IsNullOrEmpty(param.Trim()))
            {
                string[] ids = param.Split(',', StringSplitOptions.None);
                for (int i = 0; i < ((IEnumerable<string>)ids).Count<string>(); ++i)
                {
                    if (!string.IsNullOrEmpty(ids[i].Trim()))
                    {
                        int id = 0;
                        if (int.TryParse(ids[i], out id) && id != 0)
                        {
                            trafficjob entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
                            num = entity.group_id;
                            if (entity.switch_on)
                                entity.switch_on = false;
                            homeController._context.trafficjob.Update(entity);
                        }
                    }
                }
                homeController._context.SaveChanges();
                ids = (string[])null;
            }
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = num
            });
        }

        public async Task<IActionResult> Start(int id)
        {
            HomeController homeController = this;
            int groupid = -1;
            if (id != 0)
            {
                trafficjob entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
                if (entity != null)
                {
                    groupid = entity.group_id;
                    if (entity.switch_on)
                    {
                        entity.switch_on = false;
                        homeController._context.trafficjob.Update(entity);
                        homeController._context.SaveChanges();
                        Startup.RemoveScheduleForToday(homeController._context, entity.id);
                    }
                    else
                    {
                        entity.switch_on = true;
                        homeController._context.trafficjob.Update(entity);
                        homeController._context.SaveChanges();
                        Startup.AddScheduleForToday(homeController._context, entity.id);
                    }
                }
            }
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = groupid
            });
        }

        private async Task<string> GetInitToken(string authentication_code)
        {
            string str1 = "http://eemg.the-search-console.com:555/Home/GoogleLoginCallBack";
            string str2 = "authorization_code";
            return await (await new HttpClient().PostAsync("https://www.googleapis.com/oauth2/v4/token", (HttpContent)new StringContent(string.Format("{{\"code\":\"{0}\",\"client_id\":\"{1}\",\"client_secret\":\"{2}\",\"redirect_uri\":\"{3}\",\"grant_type\":\"{4}\"}}", (object)authentication_code, (object)Startup.client_id, (object)Startup.client_secret, (object)str1, (object)str2), Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
        }

        public async Task<IActionResult> GoogleLoginCallBack()
        {
            HomeController homeController = this;
            IQueryCollection query = homeController.HttpContext.Request.Query;
            if (query.ContainsKey("code"))
            {
                string[] source = ((string)query["state"]).Split('_', StringSplitOptions.None);
                int id = -1;
                string user_id = "";
                int type = 0;
                if (((IEnumerable<string>)source).Count<string>() > 0)
                    int.TryParse(source[0].Trim(), out type);
                if (((IEnumerable<string>)source).Count<string>() > 1)
                {
                    if (type == 3)
                    {
                        if (!int.TryParse(source[1].Trim(), out id))
                            id = 0;
                        if (((IEnumerable<string>)source).Count<string>() > 2)
                            user_id = source[2].Trim();
                    }
                    else
                        int.TryParse(source[1].Trim(), out id);
                }
                string authentication_code = (string)query["code"];
                string access_token = "";
                string refresh_token = "";
                try
                {
                    SettingManager.Logger(string.Format("GetInitToken before: RedirectURL {0} ", (object)authentication_code));
                    string initToken = await homeController.GetInitToken(authentication_code);
                    SettingManager.Logger(string.Format("GetInitToken after: contents {0} ", (object)initToken));
                    JObject jobject = JObject.Parse(initToken);
                    access_token = jobject["access_token"].ToString();
                    refresh_token = jobject["refresh_token"].ToString();
                    jobject["token_type"].ToString();
                }
                catch
                {
                }
                homeController.Response.Cookies.Append("Authorization1", access_token, new CookieOptions()
                {
                    Expires = new DateTimeOffset?((DateTimeOffset)DateTime.Now.AddMinutes(50.0))
                });
                if (type == 3)
                {
                    gmbproject project = new gmbproject();
                    project.project_name = "";
                    project.group_id = id;
                    project.refresh_token = refresh_token;
                    project.user_id = user_id;
                    myBusinessAccounts businessAccounts = JsonConvert.DeserializeObject<myBusinessAccounts>(await (await new HttpClient().GetAsync("https://mybusiness.googleapis.com/v4/accounts/?access_token=" + access_token)).Content.ReadAsStringAsync());
                    this.ViewBag.AccountName = "";
                    if (businessAccounts.accounts.Count > 0)
                        project.project_name = businessAccounts.accounts[0].accountName;
                    homeController.Response.Cookies.Append("Refresh", refresh_token);
                    homeController._context.Add<gmbproject>(project);
                    homeController._context.SaveChanges();
                    project = (gmbproject)null;
                }
                return type != 1 ? (type != 2 ? (type != 3 ? (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                {
                    id = id
                }) : (IActionResult)homeController.RedirectToAction("GMBProject", (object)new
                {
                    id = id,
                    userid = user_id
                })) : (IActionResult)homeController.RedirectToAction("CreateFromGooglemyBusiness", (object)new
                {
                    id = id
                })) : (IActionResult)homeController.RedirectToAction("CreateFromGoogle", (object)new
                {
                    id = id
                });
            }
            int num = 1;
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = num
            });
        }

        public async Task<IActionResult> DeletePost(string postname = "", string projectid = "")
        {
            HomeController homeController = this;
            string cookie = homeController.Request.Cookies["Authorization1"];
            if (!string.IsNullOrEmpty(cookie))
            {
                HttpClient httpClient = new HttpClient();
                if (!string.IsNullOrEmpty(postname))
                {
                    string str1 = string.Format("https://mybusiness.googleapis.com/v4/{0}?access_token={1}", (object)postname, (object)cookie);
                    string str2 = await (await httpClient.DeleteAsync(str1)).Content.ReadAsStringAsync();
                }
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation", (object)new
            {
                projectid = projectid
            });
        }

        public async Task<IActionResult> GMBStartJourney(
          string id = "",
          string projectid = "",
          string locationname = "",
          string websiteUrl = "",
          string contentSnipet = "",
          string folderid = "")
        {
            HomeController homeController = this;
            if (!string.IsNullOrEmpty(homeController.Request.Cookies["Authorization1"]))
            {
                int project_id = 0;
                int.TryParse(projectid, out project_id);
                if (homeController._context.gmbproject.FirstOrDefault<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.id == project_id)) != null)
                {
                    trafficjob entity = new trafficjob();
                    entity.job_name = locationname + " Automatic Journey";
                    entity.proxy_setting = "";
                    entity.switch_on = true;
                    entity.user_id = id;
                    entity.journey_option = "Company";
                    entity.search_term = locationname;
                    entity.search_engine = trafficjob.SEARCH_ENGINE.EN;
                    entity.target_url = websiteUrl;
                    entity.name_business = contentSnipet.Replace("%%%", "&");
                    entity.group_id = Convert.ToInt32(folderid);
                    entity.session_count = 1;
                    homeController._context.Add<trafficjob>(entity);
                    homeController._context.SaveChanges();
                    homeController._context.Entry<trafficjob>(entity).Reload();
                    Startup.AddScheduleForToday(homeController._context, entity.id, true);
                }
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation", (object)new
            {
                id = id,
                projectid = projectid
            });
        }

        public async Task<IActionResult> GMBCreateJourney(
          string id = "",
          string projectid = "",
          string locationname = "",
          string websiteUrl = "",
          string contentSnipet = "",
          string folderid = "")
        {
            HomeController homeController = this;
            if (!string.IsNullOrEmpty(homeController.Request.Cookies["Authorization1"]))
            {
                int project_id = 0;
                int.TryParse(projectid, out project_id);
                if (homeController._context.gmbproject.FirstOrDefault<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.id == project_id)) != null)
                    return (IActionResult)homeController.RedirectToAction("Create", (object)new
                    {
                        id = folderid,
                        job_name = (locationname + " Automatic Journey"),
                        proxy_setting = "",
                        switch_on = true,
                        user_id = id,
                        journey_option = "Company",
                        search_term = locationname,
                        search_engine = trafficjob.SEARCH_ENGINE.EN,
                        target_url = websiteUrl,
                        name_business = contentSnipet.Replace("%%%", "&")
                    });
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation", (object)new
            {
                id = id,
                projectid = projectid
            });
        }

        public async Task<IActionResult> T1CreateJourney(
          string id = "",
          string projectid = "",
          string locationname = "",
          string websiteUrl = "",
          string contentSnipet = "",
          string folderid = "",
          string secondurl = "",
          string firsturls = "")
        {
            HomeController homeController = this;
            if (!string.IsNullOrEmpty(homeController.Request.Cookies["Authorization1"]))
            {
                int project_id = 0;
                int.TryParse(projectid, out project_id);
                if (homeController._context.gmbproject.FirstOrDefault<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.id == project_id)) != null)
                    return (IActionResult)homeController.RedirectToAction("Create", (object)new
                    {
                        id = Convert.ToInt32(folderid),
                        job_name = (locationname + " Automatic Journey"),
                        proxy_setting = "",
                        switch_on = true,
                        user_id = id,
                        journey_option = "T1 Company",
                        search_term = secondurl,
                        search_engine = trafficjob.SEARCH_ENGINE.EN,
                        target_url = websiteUrl,
                        name_business = firsturls.Replace(",", "\r\n"),
                        session_count = 1,
                        snippet_content = contentSnipet.Replace("%%%", "&")
                    });
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation", (object)new
            {
                id = id,
                projectid = projectid
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(string name = "", string summary = "")
        {
            HomeController homeController = this;
            string cookie = homeController.Request.Cookies["Authorization1"];
            if (!string.IsNullOrEmpty(cookie))
            {
                string str = string.Format("{{\"name\": \"{0}\",\"summary\": \"{1}\"}}", (object)name, (object)summary);
                WebRequest webRequest = WebRequest.Create(string.Format("https://mybusiness.googleapis.com/v4/{0}?access_token={1}&updateMask=summary", (object)name, (object)cookie));
                webRequest.Method = "PATCH";
                webRequest.ContentType = "application/json; charset=utf-8";
                using (StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    ((TextWriter)streamWriter).Write(str);
                    ((TextWriter)streamWriter).Flush();
                }
                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(((WebResponse)response).GetResponseStream()))
                        ((TextReader)streamReader).ReadToEnd();
                }
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation");
        }

        public async Task<IActionResult> EditPost(string postname = "")
        {
            HomeController homeController = this;
            string cookie = homeController.Request.Cookies["Authorization1"];
            if (string.IsNullOrEmpty(cookie))
                return (IActionResult)homeController.RedirectToAction("GMBLocation");
            myBusinessPost model = JsonConvert.DeserializeObject<myBusinessPost>(await (await new HttpClient().GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}?access_token={1}", (object)postname, (object)cookie))).Content.ReadAsStringAsync());
            return (IActionResult)homeController.View((object)model);
        }

        public async Task<IActionResult> RemoveGMB(int id, string userid, string projectid)
        {
            HomeController homeController = this;
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            gmbproject entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<gmbproject>((IQueryable<gmbproject>)homeController._context.gmbproject, (Expression<Func<gmbproject, bool>>)(m => m.id == id), new CancellationToken());
            if (entity == null)
                return (IActionResult)homeController.NotFound();
            homeController._context.gmbproject.Remove(entity);
            homeController._context.SaveChanges();
            return (IActionResult)homeController.RedirectToAction("GMBProject", (object)new
            {
                id = projectid,
                userid = userid
            });
        }

        public async Task<IActionResult> AddGMB(string id = "", string userid = "")
        {
            HomeController homeController = this;
            string cookie1 = homeController.Request.Cookies["Authorization1"];
            string cookie2 = homeController.Request.Cookies["Refresh"];
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str);
            homeController.User.IsInRole("Admin");
            string url = string.Format("https://accounts.google.com/o/oauth2/v2/auth?scope=https://www.googleapis.com/auth/plus.business.manage https://www.googleapis.com/auth/business.manage&redirect_uri=http://eemg.the-search-console.com:555/Home/GoogleLoginCallBack&include_granted_scopes=true&access_type=offline&response_type=code&client_id={0}&state={1}_{2}_{3}", (object)Startup.client_id, (object)3, (object)id, (object)userid);
            SettingManager.Logger(string.Format("AddGMB: RedirectURL {0} ", (object)url));
            return (IActionResult)homeController.Redirect(url);
        }

        public async Task<IActionResult> SyncGMB(string id, string userid) => (IActionResult)this.RedirectToAction("GMBProject", (object)new
        {
            id = id,
            userid = userid
        });

        public async Task<IActionResult> CreateGMBProject(string id)
        {
            HomeController homeController = this;
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str);
            homeController.User.IsInRole("Admin");
            this.ViewBag.userid = id;
            return (IActionResult)homeController.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGMBProject(gmbproject gmbproject)
        {
            HomeController homeController = this;
            homeController._context.Add<gmbproject>(gmbproject);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.RedirectToAction("GMBIndex", (object)new
            {
                id = gmbproject.user_id
            });
        }

        public async Task<IActionResult> RemoveGMBProject(int id)
        {
            HomeController homeController = this;
            IEnumerable<gmbproject> listAsync = (IEnumerable<gmbproject>)await EntityFrameworkQueryableExtensions.ToListAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(m => m.id == id || m.group_id == id)), new CancellationToken());
            homeController._context.gmbproject.RemoveRange(listAsync);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.RedirectToAction("GMBIndex", (object)new
            {
                id = id
            });
        }

        public async Task<IActionResult> GMBIndex(string id)
        {
            HomeController homeController = this;
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            homeController.User.IsInRole("Admin");
            try
            {
                IEnumerable<gmbproject> listAsync = (IEnumerable<gmbproject>)await EntityFrameworkQueryableExtensions.ToListAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.user_id == id && w.group_id == -1)), new CancellationToken());
                this.ViewBag.Userid = id;
                return (IActionResult)homeController.View((object)listAsync);
            }
            catch (Exception ex)
            {
                return (IActionResult)homeController.View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> BindFolder(string folderid, string refreshtoken)
        {
            HomeController homeController = this;
            gmbproject entity = homeController._context.gmbproject.FirstOrDefault<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.refresh_token == refreshtoken));
            if (entity == null)
                return (IActionResult)homeController.Json((object)new
                {
                    message = "Refresh_Token is invalid."
                });
            int result = 0;
            int.TryParse(folderid, out result);
            if (result == 0)
                return (IActionResult)homeController.Json((object)new
                {
                    message = "Target folder is not exist."
                });
            entity.folder_id = result;
            homeController._context.Update<gmbproject>(entity);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.Json((object)new
            {
                message = "Binding Folder is successed"
            });
        }

        public async Task<IActionResult> GMBProject(int id, string userid)
        {
            HomeController homeController = this;
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            homeController.User.IsInRole("Admin");
            IEnumerable<gmbproject> resultprojects = (IEnumerable<gmbproject>)await EntityFrameworkQueryableExtensions.ToListAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(w => w.group_id == id)), new CancellationToken());
            List<SelectListItem> list = (await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(homeController._context.trafficjob.Where<trafficjob>((Expression<Func<trafficjob, bool>>)(w => w.group_id == -1 && w.user_id == userid)), new CancellationToken())).Select<trafficjob, SelectListItem>((Func<trafficjob, SelectListItem>)(x => new SelectListItem()
            {
                Text = x.job_name,
                Value = x.id.ToString()
            })).ToList<SelectListItem>();
            this.ViewBag.FolderList = new SelectList((IEnumerable)list, "Value", "Text", (object)"0");
            this.ViewBag.Userid = userid;
            this.ViewBag.Projectid = id;
            IActionResult actionResult = (IActionResult)homeController.View((object)resultprojects);
            resultprojects = (IEnumerable<gmbproject>)null;
            return actionResult;
        }

        public async Task<IActionResult> GMBLocation(string id, string projectid = "", string refresh = "")
        {
            HomeController homeController = this;
            string authorization = "";
            string refreshtoken = homeController.Request.Cookies["Refresh"];
            string str1 = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str1);
            homeController.User.IsInRole("Admin");
            gmbproject gmbproject1 = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(i => i.id == Convert.ToInt32(projectid))), new CancellationToken());
            if (gmbproject1 != null)
            {
                this.ViewBag.AccountName = "Project - " + gmbproject1.project_name;
            }
            gmbproject gmbproject2 = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(i => i.group_id == Convert.ToInt32(projectid))), new CancellationToken());
            if (gmbproject2 != null)
                refreshtoken = gmbproject2.refresh_token;
            if (string.IsNullOrEmpty(refreshtoken) && string.IsNullOrEmpty(refresh))
                return (IActionResult)homeController.RedirectToAction("GMBIndex", (object)new
                {
                    id = id
                });
            if (string.IsNullOrEmpty(refresh))
                refresh = refreshtoken;
            if (string.IsNullOrEmpty(authorization) || string.IsNullOrEmpty(refreshtoken) || refreshtoken != refresh)
            {
                try
                {
                    JObject jobject = JObject.Parse(await Startup.GetRefreshedToken(refresh));
                    string str2 = jobject["access_token"].ToString();
                    jobject["token_type"].ToString();
                    homeController.Response.Cookies.Append("Authorization1", str2, new CookieOptions()
                    {
                        Expires = new DateTimeOffset?((DateTimeOffset)DateTime.Now.AddMinutes(50.0))
                    });
                    homeController.Response.Cookies.Append("Refresh", refresh);
                    authorization = str2;
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(authorization))
                return (IActionResult)homeController.RedirectToAction("GMBIndex", (object)new
                {
                    id = id
                });
            HttpClient client = new HttpClient();
            myBusinessAccounts businessAccounts = JsonConvert.DeserializeObject<myBusinessAccounts>(await (await client.GetAsync("https://mybusiness.googleapis.com/v4/accounts/?access_token=" + authorization)).Content.ReadAsStringAsync());
            List<myBusinessLocation> locationList = new List<myBusinessLocation>();
            foreach (myBusinessAccount account in businessAccounts.accounts)
            {
                myBusinessLocations businessLocations = JsonConvert.DeserializeObject<myBusinessLocations>(await (await client.GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/locations?access_token={1}", (object)account.name, (object)authorization))).Content.ReadAsStringAsync());
                if (businessLocations != null && businessLocations.locations != null)
                    locationList.AddRange((IEnumerable<myBusinessLocation>)businessLocations.locations);
            }

            this.ViewBag.Userid = id;
            this.ViewBag.Projectid = projectid;
            return (IActionResult)homeController.View((object)locationList);
        }

        public async Task<IActionResult> GMBPost(
          string id,
          string refresh = "",
          string locationname = "",
          string projectid = "")
        {
            HomeController homeController = this;
            string authorization = homeController.Request.Cookies["Authorization1"];
            string refreshtoken = homeController.Request.Cookies["Refresh"];
            if (string.IsNullOrEmpty(refreshtoken) && string.IsNullOrEmpty(refresh))
                return (IActionResult)homeController.RedirectToAction("GMBIndex", (object)new
                {
                    id = id
                });
            string str1 = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str1);
            homeController.User.IsInRole("Admin");
            if (string.IsNullOrEmpty(refresh))
                refresh = refreshtoken;
            if (string.IsNullOrEmpty(authorization) || string.IsNullOrEmpty(refreshtoken) || refreshtoken != refresh)
            {
                JObject jobject = JObject.Parse(await Startup.GetRefreshedToken(refresh));
                string str2 = jobject["access_token"].ToString();
                jobject["token_type"].ToString();
                homeController.Response.Cookies.Append("Authorization1", str2, new CookieOptions()
                {
                    Expires = new DateTimeOffset?((DateTimeOffset)DateTime.Now.AddMinutes(50.0))
                });
                homeController.Response.Cookies.Append("Refresh", refresh);
                authorization = str2;
            }
            HttpClient client = new HttpClient();
            this.ViewBag.AccountName = "";
            this.ViewBag.AccountName1 = "";
            int length = locationname.IndexOf("/locations");
            if (length > 0)
            {
                foreach (myBusinessLocation location in JsonConvert.DeserializeObject<myBusinessLocations>(await (await client.GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/locations?access_token={1}", (object)locationname.Substring(0, length), (object)authorization))).Content.ReadAsStringAsync()).locations)
                {
                    if (location.name == locationname)
                    {
                        this.ViewBag.AccountName = location.locationName;
                        this.ViewBag.AccountName1 = location.locationName;
                        break;
                    }
                }
            }
            myBusinessPosts localposts = JsonConvert.DeserializeObject<myBusinessPosts>(await (await client.GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts?access_token={1}", (object)locationname, (object)authorization))).Content.ReadAsStringAsync());
            List<myBusinessPost> postList = new List<myBusinessPost>();
            string firsturls = "";
            if (localposts != null && localposts.localPosts != null)
            {
                foreach (myBusinessPost post in localposts.localPosts)
                {
                    DateTime dateTime = DateTime.Now;
                    dateTime = dateTime.AddMonths(-3);
                    string str3 = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
                    dateTime = DateTime.Now;
                    string str4 = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
                    if (post.callToAction != null && post.callToAction.url != null)
                        firsturls = firsturls + post.callToAction.url + ",";
                    string str5 = string.Format("{{\"localPostNames\": [\"{0}\"],\"basicRequest\": {{\"metricRequests\": [{{\"metric\": \"ALL\",\"options\": [\"AGGREGATED_TOTAL\"]}}],\"timeRange\": {{\"startTime\": \"{1}\",\"endTime\": \"{2}\"}}}}}}", (object)post.name, (object)str3, (object)str4);
                    myBusinessPostInsight businessPostInsight = JsonConvert.DeserializeObject<myBusinessPostInsight>(await (await client.PostAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts:reportInsights?access_token={1}", (object)locationname, (object)authorization), (HttpContent)new StringContent(str5, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync());
                    if (businessPostInsight != null && businessPostInsight.localPostMetrics != null && businessPostInsight.localPostMetrics.Count > 0 && businessPostInsight.localPostMetrics[0].metricValues != null)
                    {
                        foreach (myBusinessmetricValues metricValue in businessPostInsight.localPostMetrics[0].metricValues)
                        {
                            if (metricValue.totalValue != null)
                            {
                                if (string.IsNullOrEmpty(metricValue.metric))
                                    post.clicks = metricValue.totalValue.value;
                                else if (metricValue.metric == "LOCAL_POST_VIEWS_SEARCH")
                                    post.views = metricValue.totalValue.value;
                            }
                        }
                    }
                }
                postList.AddRange((IEnumerable<myBusinessPost>)localposts.localPosts);
            }
            if (firsturls != "")
                firsturls = firsturls.Substring(0, firsturls.Length - 1);
            List<trafficjob> trafficjobList = new List<trafficjob>();
            List<trafficjob> list = homeController._context.trafficjob.Where<trafficjob>((Expression<Func<trafficjob, bool>>)(i => i.user_id == id && i.group_id == -1)).ToList<trafficjob>();
            this.ViewBag.listofitems = list;
            this.ViewBag.firsturls = firsturls;
            this.ViewBag.PostList = postList;
            this.ViewBag.Userid = id;
            this.ViewBag.Projectid = projectid;
            this.ViewBag.locationname = locationname;
            return (IActionResult)homeController.View();
        }

        public async Task<IActionResult> CreateFromGooglemyBusiness(
          int id,
          string post_location = "",
          string post_summary = "",
          string post_actiontype = "",
          string post_actionurl = "",
          string post_mediaformat = "",
          string post_mediaurl = "")
        {
            HomeController homeController = this;
            string authorization = homeController.Request.Cookies["Authorization1"];
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            bool IsAdmin = homeController.User.IsInRole("Admin");
            if (string.IsNullOrEmpty(authorization))
            {
                if (!IsAdmin)
                {
                    int nCreatedCount = 0;
                    DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                    Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                    foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                    {
                        if (trafficjob2.user_id == currentUserID)
                            ++nCreatedCount;
                    }
                    if (nCreatedCount >= user.MaxJourneyCount)
                        return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                        {
                            id = id
                        });
                }
                string url = string.Format("https://accounts.google.com/o/oauth2/v2/auth?scope=https://www.googleapis.com/auth/plus.business.manage&redirect_uri=http://eemg.the-search-console.com:555/Home/GoogleLoginCallBack&response_type=code&client_id={0}&state={1}_{2}", (object)Startup.client_id, (object)2, (object)id);
                return (IActionResult)homeController.Redirect(url);
            }
            HttpClient client = new HttpClient();
            myBusinessAccounts businessAccounts = JsonConvert.DeserializeObject<myBusinessAccounts>(await (await client.GetAsync("https://mybusiness.googleapis.com/v4/accounts/?access_token=" + authorization)).Content.ReadAsStringAsync());
            this.ViewBag.AccountList = businessAccounts.accounts;
            List<myBusinessLocation> locationList = new List<myBusinessLocation>();
            foreach (myBusinessAccount account in businessAccounts.accounts)
              locationList.AddRange((IEnumerable<myBusinessLocation>) JsonConvert.DeserializeObject<myBusinessLocations>(await (await client.GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/locations?access_token={1}", (object) account.name, (object) authorization))).Content.ReadAsStringAsync()).locations);
            this.ViewBag.LocationList = locationList;
            if (!string.IsNullOrEmpty(post_location) && !string.IsNullOrEmpty(post_summary) && !string.IsNullOrEmpty(post_actiontype) && !string.IsNullOrEmpty(post_actionurl) && !string.IsNullOrEmpty(post_mediaformat) && !string.IsNullOrEmpty(post_mediaurl))
            {
                string str1 = string.Format("{{\"languageCode\": \"en-US\",\"summary\": \"{0}\",\"callToAction\": {{\"actionType\": \"{1}\",\"url\": \"{2}\",}},\"media\": [{{\"mediaFormat\": \"{3}\",\"sourceUrl\": \"{4}\",}}],}}", (object)post_summary, (object)post_actiontype, (object)post_actionurl, (object)post_mediaformat, (object)post_mediaurl);
                string str2 = await (await client.PostAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts?access_token={1}", (object)post_location, (object)authorization), (HttpContent)new StringContent(str1, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
            }
            List<SelectListItem> list = ((IEnumerable<string>)SettingManager.JoureyOptions).Select<string, SelectListItem>((Func<string, SelectListItem>)(x => new SelectListItem()
            {
                Text = x,
                Value = x
            })).ToList<SelectListItem>();
            List<SelectListItem> items = new List<SelectListItem>();
            if (!IsAdmin)
            {
                if (string.IsNullOrEmpty(user.JourneyOptionList))
                {
                    items = list.ToList<SelectListItem>();
                }
                else
                {
                    string[] source = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                    foreach (SelectListItem selectListItem in list)
                    {
                        bool flag = false;
                        string str = selectListItem.Text.Replace(" with Google Form", "");
                        for (int index = 0; index < ((IEnumerable<string>)source).Count<string>(); ++index)
                        {
                            if (str == source[index])
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                            items.Add(selectListItem);
                    }
                }
                this.ViewBag.MaxSessionCount = user.MaxRunCountPerDay;
            }
            else
            {
                items = list.ToList<SelectListItem>();
                this.ViewBag.MaxSessionCount = 99999;
            }
            this.ViewBag.JourneyOption = new SelectList((IEnumerable)items, "Value", "Text", (object)"0");
            this.ViewBag.Parent = id;
            return (IActionResult)homeController.View();
        }

        public async Task<IActionResult> CreateFromGoogle(int id, string startdate = "", string enddate = "")
        {
            HomeController homeController = this;
            string authorization = homeController.Request.Cookies["Authorization1"];
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            bool IsAdmin = homeController.User.IsInRole("Admin");
            if (string.IsNullOrEmpty(authorization))
            {
                if (!IsAdmin)
                {
                    int nCreatedCount = 0;
                    DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                    Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                    foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                    {
                        if (trafficjob2.user_id == currentUserID)
                            ++nCreatedCount;
                    }
                    if (nCreatedCount >= user.MaxJourneyCount)
                        return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                        {
                            id = id
                        });
                }
                string url = string.Format("https://accounts.google.com/o/oauth2/v2/auth?scope=https://www.googleapis.com/auth/webmasters https://www.googleapis.com/auth/webmasters.readonly&redirect_uri=http://eemg.the-search-console.com:555/Home/GoogleLoginCallBack&response_type=code&client_id={0}&state={1}_{2}", (object)Startup.client_id, (object)1, (object)id);
                return (IActionResult)homeController.Redirect(url);
            }
            HttpClient client = new HttpClient();
            string str1 = await (await client.GetAsync("https://www.googleapis.com/webmasters/v3/sites?access_token=" + authorization)).Content.ReadAsStringAsync();
            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.AddMonths(-3);
            string StartTime = dateTime.ToString("yyyy-MM-dd");
            string EndTime = DateTime.Now.ToString("yyyy-MM-dd");
            if (!string.IsNullOrEmpty(startdate) && !string.IsNullOrEmpty(enddate))
            {
                StartTime = startdate;
                EndTime = enddate;
            }
            List<SearchConsoleData> searchconsoledatas = new List<SearchConsoleData>();
            try
            {
                foreach (SiteOjbect siteOjbect in JsonConvert.DeserializeObject<SiteListObject>(str1).siteEntry)
                {
                    SiteOjbect site = siteOjbect;
                    if (!(site.permissionLevel != "siteOwner"))
                    {
                        string str2 = string.Format("{{\"startDate\":\"{0}\",\"endDate\":\"{1}\",\"searchType\":\"web\",\"aggregationType\":\"byPage\",\"dimensions\":[{2}]}}", (object)StartTime, (object)EndTime, (object)"\"page\",\"query\"");
                        if (!site.siteUrl.EndsWith("/"))
                            site.siteUrl += "/";
                        string str3 = await (await client.PostAsync(string.Format("https://www.googleapis.com/webmasters/v3/sites/{0}/searchAnalytics/query?access_token={1}", (object)HttpUtility.UrlEncode(site.siteUrl), (object)authorization), (HttpContent)new StringContent(str2, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
                        SearchConsoleData searchConsoleData = new SearchConsoleData();
                        searchConsoleData.SiteUrl = site.siteUrl;
                        searchConsoleData.SiteData = new List<SiteUrlData>();
                        searchconsoledatas.Add(searchConsoleData);
                        SiteResult siteResult = JsonConvert.DeserializeObject<SiteResult>(str3);
                        if (siteResult.rows != null)
                        {
                            foreach (Row row in siteResult.rows)
                            {
                                SiteUrlData siteUrlData = new SiteUrlData();
                                if (row.keys.Count > 0)
                                    siteUrlData.SiteSubUrl = row.keys[0];
                                if (row.keys.Count > 1)
                                    siteUrlData.Query = row.keys[1];
                                siteUrlData.clicks = row.clicks;
                                siteUrlData.impressions = row.impressions;
                                siteUrlData.ctr = row.ctr;
                                siteUrlData.position = row.position;
                                searchConsoleData.SiteData.Add(siteUrlData);
                            }
                        }
                        site = (SiteOjbect)null;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            this.ViewBag.StartTime = StartTime;
            this.ViewBag.EndTime = EndTime;
            this.ViewBag.Searchconsoledatas = searchconsoledatas;
            List<SelectListItem> list = ((IEnumerable<string>)SettingManager.JoureyOptions).Select<string, SelectListItem>((Func<string, SelectListItem>)(x => new SelectListItem()
            {
                Text = x,
                Value = x
            })).ToList<SelectListItem>();
            List<SelectListItem> items = new List<SelectListItem>();
            if (!IsAdmin)
            {
                if (string.IsNullOrEmpty(user.JourneyOptionList))
                {
                    items = list.ToList<SelectListItem>();
                }
                else
                {
                    string[] source = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                    foreach (SelectListItem selectListItem in list)
                    {
                        bool flag = false;
                        string str4 = selectListItem.Text.Replace(" with Google Form", "");
                        for (int index = 0; index < ((IEnumerable<string>)source).Count<string>(); ++index)
                        {
                            if (str4 == source[index])
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                            items.Add(selectListItem);
                    }
                }
                this.ViewBag.MaxSessionCount = user.MaxRunCountPerDay;
            }
            else
            {
                items = list.ToList<SelectListItem>();
                this.ViewBag.MaxSessionCount = 99999;            }
            this.ViewBag.JourneyOption = new SelectList((IEnumerable)items, "Value", "Text", (object)"0");
            this.ViewBag.Parent = id;
            return (IActionResult)homeController.View();
        }

        public async Task<IActionResult> GMBAccount(string id)
        {
            HomeController homeController = this;
            List<gmbproject> gmbprojectList = new List<gmbproject>();
            List<gmbproject> list = homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(i => i.group_id != 0 && i.group_id != -1 && i.user_id == id)).ToList<gmbproject>();
            this.ViewBag.listofitems = list;
            this.ViewBag.userid = id;
            return (IActionResult)homeController.View();
        }

        [HttpPost]
        public async Task<JsonResult> ChangeGMB(int id)
        {
            HomeController homeController = this;
            gmbproject gmbproject = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(i => i.id == id)), new CancellationToken());
            string refresh = "";
            refresh = gmbproject.refresh_token;
            string authorization = "";
            int gid = gmbproject.group_id;
            string pname = "";
            pname = (await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<gmbproject>(homeController._context.gmbproject.Where<gmbproject>((Expression<Func<gmbproject, bool>>)(i => i.id == gid)), new CancellationToken())).project_name;
            if (string.IsNullOrEmpty(refresh))
                return homeController.Json((object)"{\"data\":\"failed\"}");
            string str1 = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str1);
            homeController.User.IsInRole("Admin");
            try
            {
                JObject jobject = JObject.Parse(await Startup.GetRefreshedToken(refresh));
                string str2 = jobject["access_token"].ToString();
                jobject["token_type"].ToString();
                homeController.Response.Cookies.Append("Authorization1", str2, new CookieOptions()
                {
                    Expires = new DateTimeOffset?((DateTimeOffset)DateTime.Now.AddMinutes(50.0))
                });
                homeController.Response.Cookies.Append("Refresh", refresh);
                authorization = str2;
            }
            catch
            {
                return homeController.Json((object)"{\"data\":\"failed\"}");
            }
            HttpClient client = new HttpClient();
            string str3 = await (await client.GetAsync("https://mybusiness.googleapis.com/v4/accounts/?access_token=" + authorization)).Content.ReadAsStringAsync();
            string result = "";
            myBusinessAccounts businessAccounts = JsonConvert.DeserializeObject<myBusinessAccounts>(str3);
            List<myBusinessLocation> locationList = new List<myBusinessLocation>();
            foreach (myBusinessAccount account in businessAccounts.accounts)
            {
                myBusinessLocations businessLocations = JsonConvert.DeserializeObject<myBusinessLocations>(await (await client.GetAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/locations?access_token={1}", (object)account.name, (object)authorization))).Content.ReadAsStringAsync());
                if (businessLocations != null)
                    locationList.AddRange((IEnumerable<myBusinessLocation>)businessLocations.locations);
            }
            if (locationList.Count > 0)
            {
                result = "{\"data\":[";
                foreach (myBusinessLocation businessLocation in locationList)
                    result = result + "{\"pname\":\"" + pname + "\",\"name\":\"" + businessLocation.locationName + "\"},";
                result = result.Substring(0, result.Length - 1);
                result += "]}";
            }
            return homeController.Json((object)result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGMBOfferPost(
          string locationnameforone = "",
          string time_zone = "",
          string post_time = "",
          string post_languagecode = "",
          string post_summary = "",
          string post_actiontype = "",
          string post_actionurl = "",
          string post_mediaformat = "",
          string post_mediaurl = "",
          string projectId = "")
        {
            HomeController homeController = this;
            string cookie1 = homeController.Request.Cookies["Authorization1"];
            if (!string.IsNullOrEmpty(cookie1))
            {
                HttpClient httpClient = new HttpClient();
                if (!string.IsNullOrEmpty(locationnameforone) && !string.IsNullOrEmpty(post_time) && !string.IsNullOrEmpty(post_languagecode) && !string.IsNullOrEmpty(post_summary) && !string.IsNullOrEmpty(post_actiontype) && !string.IsNullOrEmpty(post_actionurl) && !string.IsNullOrEmpty(post_mediaformat) && !string.IsNullOrEmpty(post_mediaurl))
                {
                    DateTime dateTime = DateTime.MinValue;
                    try
                    {
                        dateTime = DateTime.Parse(post_time);
                        if (string.IsNullOrEmpty(time_zone))
                            time_zone = "GMT Standard Time";
                        TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(time_zone);
                        dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, systemTimeZoneById);
                    }
                    catch
                    {
                    }
                    if (DateTime.Now >= dateTime)
                    {
                        string str1 = string.Format("{{\"languageCode\": \"{0}\",\"summary\": \"{1}\",\"callToAction\": {{\"actionType\": \"{2}\",\"url\": \"{3}\",}},\"media\": [{{\"mediaFormat\": \"{4}\",\"sourceUrl\": \"{5}\",}}],}}", (object)post_languagecode, (object)post_summary, (object)post_actiontype, (object)post_actionurl, (object)post_mediaformat, (object)post_mediaurl);
                        string str2 = string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts?access_token={1}", (object)locationnameforone, (object)cookie1);
                        string str3 = await (await httpClient.PostAsync(str2, (HttpContent)new StringContent(str1, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
                    }
                    else
                    {
                        string cookie2 = homeController.Request.Cookies["Refresh"];
                        if (!string.IsNullOrEmpty(cookie2))
                        {
                            homeController._context.Add<gmbpost_query>(new gmbpost_query()
                            {
                                locationname = locationnameforone,
                                refreshtoken = cookie2,
                                time = dateTime,
                                languagecode = post_languagecode,
                                summary = post_summary,
                                actiontype = post_actiontype,
                                actionurl = post_actionurl,
                                mediaformat = post_mediaformat,
                                mediaurl = post_mediaurl
                            });
                            homeController._context.SaveChanges();
                        }
                    }
                }
            }
            return (IActionResult)homeController.RedirectToAction("GMBLocation", (object)new
            {
                projectid = projectId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGMBOfferPostBatch(
          List<IFormFile> files,
          string locationnameforbatch)
        {
            HomeController homeController = this;
            string authorization = homeController.Request.Cookies["Authorization1"];
            string refreshtoken = homeController.Request.Cookies["Refresh"];
            HttpClient client = new HttpClient();
            if (!string.IsNullOrEmpty(authorization) && !string.IsNullOrEmpty(locationnameforbatch))
            {
                files.Sum<IFormFile>((Func<IFormFile, long>)(f => f.Length));
                string DirectoryPath = Directory.GetCurrentDirectory() + "/UploadFiles/";
                try
                {
                    foreach (IFormFile file in files)
                    {
                        if (file.Length > 0L)
                        {
                            string filePath = DirectoryPath + file.FileName;
                            using (FileStream stream = new FileStream(filePath, (FileMode)2))
                                await file.CopyToAsync((Stream)stream, new CancellationToken());
                            int nIndex_time = 0;
                            int nIndex_timezone = 0;
                            int nIndex_language = 0;
                            int nIndex_summary = 0;
                            int nIndex_actiontype = 0;
                            int nIndex_actionurl = 0;
                            int nIndex_meidaformat = 0;
                            int nIndex_mediaurl = 0;
                            bool bIsFirstline = true;
                            foreach (string line in System.IO.File.ReadLines(filePath).ToList<string>())
                            {
                                List<string> list = SettingManager.LineSplitter(line).ToList<string>();
                                if (bIsFirstline)
                                {
                                    Dictionary<string, int> dictionary = new Dictionary<string, int>();
                                    for (int index = 0; index < list.Count; ++index)
                                        dictionary[list[index]] = index;
                                    if (dictionary.ContainsKey("Time"))
                                        nIndex_time = dictionary["Time"];
                                    if (dictionary.ContainsKey("TimeZone"))
                                        nIndex_timezone = dictionary["TimeZone"];
                                    if (dictionary.ContainsKey("Language"))
                                        nIndex_language = dictionary["Language"];
                                    if (dictionary.ContainsKey("Summary"))
                                        nIndex_summary = dictionary["Summary"];
                                    if (dictionary.ContainsKey("Action Type"))
                                        nIndex_actiontype = dictionary["Action Type"];
                                    if (dictionary.ContainsKey("Action Url"))
                                        nIndex_actionurl = dictionary["Action Url"];
                                    if (dictionary.ContainsKey("Media Format"))
                                        nIndex_meidaformat = dictionary["Media Format"];
                                    if (dictionary.ContainsKey("Media Url"))
                                        nIndex_mediaurl = dictionary["Media Url"];
                                    bIsFirstline = false;
                                }
                                else
                                {
                                    string id = list[nIndex_timezone];
                                    DateTime dateTime = DateTime.MinValue;
                                    try
                                    {
                                        dateTime = DateTime.Parse(list[nIndex_time]);
                                        if (string.IsNullOrEmpty(id))
                                            id = "GMT Standard Time";
                                        TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(id);
                                        dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, systemTimeZoneById);
                                    }
                                    catch
                                    {
                                    }
                                    string str1 = list[nIndex_language];
                                    string str2 = list[nIndex_summary];
                                    string str3 = list[nIndex_actiontype];
                                    string str4 = list[nIndex_actionurl];
                                    string str5 = list[nIndex_meidaformat];
                                    string str6 = list[nIndex_mediaurl];
                                    if (!string.IsNullOrEmpty(str1) && !string.IsNullOrEmpty(str2) && !string.IsNullOrEmpty(str3) && !string.IsNullOrEmpty(str4) && !string.IsNullOrEmpty(str5) && !string.IsNullOrEmpty(str6))
                                    {
                                        if (DateTime.Now >= dateTime)
                                        {
                                            string str7 = string.Format("{{\"languageCode\": \"{0}\",\"summary\": \"{1}\",\"callToAction\": {{\"actionType\": \"{2}\",\"url\": \"{3}\",}},\"media\": [{{\"mediaFormat\": \"{4}\",\"sourceUrl\": \"{5}\",}}],}}", (object)str1, (object)str2, (object)str3, (object)str4, (object)str5, (object)str6);
                                            string str8 = await (await client.PostAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts?access_token={1}", (object)locationnameforbatch, (object)authorization), (HttpContent)new StringContent(str7, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
                                        }
                                        else if (!string.IsNullOrEmpty(refreshtoken))
                                        {
                                            homeController._context.Add<gmbpost_query>(new gmbpost_query()
                                            {
                                                locationname = locationnameforbatch,
                                                refreshtoken = refreshtoken,
                                                time = dateTime,
                                                languagecode = str1,
                                                summary = str2,
                                                actiontype = str3,
                                                actionurl = str4,
                                                mediaformat = str5,
                                                mediaurl = str6
                                            });
                                            homeController._context.SaveChanges();
                                        }
                                    }
                                }
                            }
                            filePath = (string)null;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                DirectoryPath = (string)null;
            }
            IActionResult action = (IActionResult)homeController.RedirectToAction("GMBLocation");
            authorization = (string)null;
            refreshtoken = (string)null;
            client = (HttpClient)null;
            return action;
        }

        public async Task<IActionResult> Create(
          int id,
          string job_name = "",
          string proxy_setting = "",
          bool switch_on = true,
          string user_id = "",
          string journey_option = "",
          string search_term = "",
          trafficjob.SEARCH_ENGINE search_engine = trafficjob.SEARCH_ENGINE.EN,
          string target_url = "",
          string name_business = "",
          int session_count = 1,
          string snippet_content = "")
        {
            HomeController homeController = this;
            trafficjob trafficjob = new trafficjob();
            trafficjob.job_name = job_name;
            trafficjob.proxy_setting = proxy_setting;
            trafficjob.switch_on = switch_on;
            trafficjob.user_id = user_id;
            trafficjob.journey_option = journey_option;
            trafficjob.search_term = search_term;
            trafficjob.search_engine = search_engine;
            trafficjob.target_url = target_url;
            trafficjob.snippet_content = snippet_content.Replace("%%%", "&");
            trafficjob.name_business = name_business;
            trafficjob.session_count = 1;
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            bool IsAdmin = homeController.User.IsInRole("Admin");
            bool bGeoLocation = false;
            if (!IsAdmin)
            {
                int nCreatedCount = 0;
                DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                {
                    if (trafficjob2.user_id == currentUserID)
                        ++nCreatedCount;
                }
                if (nCreatedCount >= user.MaxJourneyCount)
                    return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                    {
                        id = id
                    });
            }
            List<SelectListItem> list1 = ((IEnumerable<string>)SettingManager.JoureyOptions).Select<string, SelectListItem>((Func<string, SelectListItem>)(x => new SelectListItem()
            {
                Text = x,
                Value = x
            })).ToList<SelectListItem>();
            List<SelectListItem> items = new List<SelectListItem>();
            if (!IsAdmin)
            {
                IsAdmin = user.EnableScheduleInterupt;
                if (string.IsNullOrEmpty(user.JourneyOptionList))
                {
                    items = list1.ToList<SelectListItem>();
                }
                else
                {
                    string[] source = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                    foreach (SelectListItem selectListItem in list1)
                    {
                        bool flag = false;
                        string str = selectListItem.Text.Replace(" with Google Form", "");
                        for (int index = 0; index < ((IEnumerable<string>)source).Count<string>(); ++index)
                        {
                            if (str == source[index])
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                            items.Add(selectListItem);
                    }
                }
                this.ViewBag.MaxSessionCount = user.MaxRunCountPerDay;
                if (user.EnableGeoLocation)
                    bGeoLocation = true;
            }
            else
            {
                bGeoLocation = true;
                items = list1.ToList<SelectListItem>();
                this.ViewBag.MaxSessionCount = 99999;
            }
            homeController.ViewData["CreateInterupt"] = (object)IsAdmin;
            homeController.ViewData["EnableGeoLocation"] = (object)bGeoLocation;
            if (journey_option != "")
            {
                this.ViewBag.JourneyOption = new SelectList((IEnumerable)items, "Value", "Text", (object)journey_option);
            }
            else
            {
                this.ViewBag.JourneyOption = new SelectList((IEnumerable)items, "Value", "Text", (object)"0");
            }
            List<Traffic.Models.location> locationList = new List<Traffic.Models.location>();
            List<Traffic.Models.location> list2 = homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == 0)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)).ToList<Traffic.Models.location>();
            this.ViewBag.List = list2;
            this.ViewBag.Parent = id;
            return (IActionResult)homeController.View((object)trafficjob);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
          [Bind(new string[] { "job_name, journey_option, target_url, exttarget_url, name_business, wild_card, search_term,location, search_engine,snippet_content, agent_kind, agent_age, session_count, group_id, proxy_setting, journey_url, form_fields, time_zone, start_time, end_time, googlemaplocal, googlemaplist,googlegeoresultselect,impressions,cookie_website" })] trafficjob trafficjob,
          bool schedule_imme,
          string superOption)
        {
            HomeController homeController = this;
            if (trafficjob.proxy_setting != null && trafficjob.proxy_setting.Length > 500)
                trafficjob.proxy_setting = trafficjob.proxy_setting.Substring(0, 499);
            trafficjob.switch_on = true;
            trafficjob.user_id = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            if (!homeController.ModelState.IsValid)
                return (IActionResult)homeController.View((object)trafficjob);
            string source1 = "";
            if (trafficjob.search_term != null)
            {
                foreach (string str in trafficjob.search_term.Split("\n", StringSplitOptions.None))
                {
                    if (!string.IsNullOrEmpty(str))
                        source1 = source1 + str.Replace("\r", "") + "|";
                }
                if (source1.Length > 0 && source1.ElementAt<char>(source1.Length - 1) == '|')
                    source1 = source1.Substring(0, source1.Length - 1);
            }
            trafficjob.search_term = source1;
            string source2 = "";
            if (trafficjob.name_business != null)
            {
                foreach (string str in trafficjob.name_business.Split("\n", StringSplitOptions.None))
                {
                    if (!string.IsNullOrEmpty(str))
                        source2 = source2 + str.Replace("\r", "") + "|";
                }
                if (source2.Length > 0 && source2.ElementAt<char>(source2.Length - 1) == '|')
                    source2 = source2.Substring(0, source2.Length - 1);
            }
            trafficjob.name_business = source2;
            if (trafficjob.journey_option == "Super G-Site")
                trafficjob.name_business = superOption;
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            if (!homeController.User.IsInRole("Admin"))
            {
                int nCreatedCount = 0;
                DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                {
                    if (trafficjob2.user_id == currentUserID)
                        ++nCreatedCount;
                }
                if (nCreatedCount >= user.MaxJourneyCount)
                    return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                    {
                        id = trafficjob.group_id
                    });
                if (trafficjob.session_count > user.MaxRunCountPerDay)
                    trafficjob.session_count = user.MaxRunCountPerDay;
                string str = trafficjob.journey_option.Replace(" with Google Form", "");
                if (!string.IsNullOrEmpty(user.JourneyOptionList))
                {
                    string[] source3 = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                    bool flag = false;
                    for (int index = 0; index < ((IEnumerable<string>)source3).Count<string>(); ++index)
                    {
                        if (str == source3[index])
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        return (IActionResult)homeController.RedirectToAction(nameof(Create), (object)new
                        {
                            id = trafficjob.group_id
                        });
                }
            }
            homeController._context.Add<trafficjob>(trafficjob);
            homeController._context.SaveChanges();
            homeController._context.Entry<trafficjob>(trafficjob).Reload();
            Startup.AddScheduleForToday(homeController._context, trafficjob.id, schedule_imme);
            IEnumerable<Traffic.Models.location> listAsync = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location, new CancellationToken());
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = trafficjob.group_id
            });
        }

        public async Task<IActionResult> Statistics(string datepicker)
        {
            HomeController homeController = this;
            DateTime browsetime = DateTime.Now;
            List<dailystate> dailystateList = new List<dailystate>();
            List<dailystateDesc> dailystatisticsDesc = new List<dailystateDesc>();
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str);
            if (!homeController.User.IsInRole("Admin"))
                return (IActionResult)homeController.NotFound();
            if (!string.IsNullOrEmpty(datepicker))
                browsetime = Convert.ToDateTime(datepicker);
            int TotalCount = 0;
            int YesterdayCount = 0;
            int TodayCount = 0;
            int AlreadyRunCount = 0;
            int AlreadySuccessCount = 0;
            int RemainCount = 0;
            try
            {
                IQueryable<dailystate> source = homeController._context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == browsetime.Date));
                Expression<Func<dailystate, DateTime>> keySelector = (Expression<Func<dailystate, DateTime>>)(o => o.predict_time);
                foreach (dailystate dailystate in await EntityFrameworkQueryableExtensions.ToListAsync<dailystate>((IQueryable<dailystate>)source.OrderBy<dailystate, DateTime>(keySelector), new CancellationToken()))
                {
                    dailystate state = dailystate;
                    ++TotalCount;
                    if (state.isoldjourney)
                        ++YesterdayCount;
                    else
                        ++TodayCount;
                    if (state.state == 2)
                    {
                        ++AlreadySuccessCount;
                        ++AlreadyRunCount;
                    }
                    else if (state.state == 1 && state.consfailcount >= 3 || state.state == 3)
                        ++AlreadyRunCount;
                    else
                        ++RemainCount;
                    dailystateDesc dailystateDesc = new dailystateDesc(state);
                    trafficjob trafficjob = (trafficjob)null;
                    try
                    {
                        trafficjob = homeController._context.trafficjob.SingleOrDefault<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == state.job_id));
                    }
                    catch
                    {
                    }
                    try
                    {
                        DisplayAttribute customAttribute = CustomAttributeExtensions.GetCustomAttribute<DisplayAttribute>((MemberInfo)trafficjob.search_engine.GetType().GetField(trafficjob.search_engine.ToString()));
                        if (trafficjob != null)
                            dailystateDesc.JobDesc = string.Format("Job Name: {0}\n\nJourney Option: {1}\n\nTarget Url: {2}\n\nSearch Term: {3}\n\nSearch Engine: {4}", (object)trafficjob.job_name, (object)trafficjob.journey_option, (object)trafficjob.target_url, (object)trafficjob.search_term, (object)customAttribute.Name);
                        dailystatisticsDesc.Add(dailystateDesc);
                    }
                    catch (Exception ex)
                    {
                        SettingManager.Logger(string.Format("Exception Statistics Inner: Message {0} Stack: {1}", (object)ex.Message, (object)ex.StackTrace));
                    }
                }
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception Statistics Outer: Message {0} Stack: {1}", (object)ex.Message, (object)ex.StackTrace));
            }
            homeController.ViewData["TotalCount"] = (object)TotalCount;
            homeController.ViewData["YesterdayCount"] = (object)YesterdayCount;
            homeController.ViewData["TodayCount"] = (object)TodayCount;
            homeController.ViewData["AlreadyRunCount"] = (object)AlreadyRunCount;
            homeController.ViewData["AlreadySuccessCount"] = (object)AlreadySuccessCount;
            homeController.ViewData["RemainCount"] = (object)RemainCount;
            ViewDataDictionary viewData1 = homeController.ViewData;
            DateTime dateTime = browsetime.Date;
            string shortDateString = dateTime.ToShortDateString();
            viewData1["date"] = (object)shortDateString;
            ViewDataDictionary viewData2 = homeController.ViewData;
            DateTime date1 = browsetime.Date;
            dateTime = DateTime.Now;
            DateTime date2 = dateTime.Date;
            // ISSUE: variable of a boxed type
            bool local = date1 == date2;
            viewData2["IsToday"] = (object)local;
            return (IActionResult)homeController.View((object)dailystatisticsDesc);
        }

        public IActionResult CreateFolder() => (IActionResult)this.View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromFile(List<IFormFile> files, int FolderId)
        {
            HomeController homeController = this;
            files.Sum<IFormFile>((Func<IFormFile, long>)(f => f.Length));
            string DirectoryPath = Directory.GetCurrentDirectory() + "/UploadFiles/";
            string str1 = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            bool IsAdmin = homeController.User.IsInRole("Admin");
            try
            {
                foreach (IFormFile file in files)
                {
                    if (file.Length > 0L)
                    {
                        string filePath = DirectoryPath + file.FileName;
                        using (FileStream stream = new FileStream(filePath, (FileMode)2))
                            await file.CopyToAsync((Stream)stream, new CancellationToken());
                        int nIndex_JobName = 0;
                        int nIndex_JourneyOption = 0;
                        int nIndex_TargetURL = 0;
                        int nIndex_ExtTargetURL = 0;
                        int nIndex_NameoftheBusiness = 0;
                        int nIndex_SearchTerm = 0;
                        int nIndex_SearchEngine = 0;
                        int nIndex_AgentKind = 0;
                        int nIndex_Agentof = 0;
                        int nIndex_TimeZone = 0;
                        int nIndex_Start = 0;
                        int nIndex_End = 0;
                        int nIndex_Count = 0;
                        int nIndex_Proxylist = 0;
                        bool bIsFirstline = true;
                        foreach (string line in System.IO.File.ReadLines(filePath).ToList<string>())
                        {
                            List<string> splits = SettingManager.LineSplitter(line).ToList<string>();
                            if (bIsFirstline)
                            {
                                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                                for (int index = 0; index < splits.Count; ++index)
                                    dictionary[splits[index]] = index;
                                if (dictionary.ContainsKey("Job Name"))
                                    nIndex_JobName = dictionary["Job Name"];
                                if (dictionary.ContainsKey("Journey Option"))
                                    nIndex_JourneyOption = dictionary["Journey Option"];
                                if (dictionary.ContainsKey("Target URL"))
                                    nIndex_TargetURL = dictionary["Target URL"];
                                if (dictionary.ContainsKey("Mobile Target URL"))
                                    nIndex_ExtTargetURL = dictionary["Mobile Target URL"];
                                if (dictionary.ContainsKey("Name of the Business"))
                                    nIndex_NameoftheBusiness = dictionary["Name of the Business"];
                                if (dictionary.ContainsKey("Search Term"))
                                    nIndex_SearchTerm = dictionary["Search Term"];
                                if (dictionary.ContainsKey("Search Engine"))
                                    nIndex_SearchEngine = dictionary["Search Engine"];
                                if (dictionary.ContainsKey("Agent Kind"))
                                    nIndex_AgentKind = dictionary["Agent Kind"];
                                if (dictionary.ContainsKey("Agent of"))
                                    nIndex_Agentof = dictionary["Agent of"];
                                if (dictionary.ContainsKey("Time Zone"))
                                    nIndex_TimeZone = dictionary["Time Zone"];
                                if (dictionary.ContainsKey("Start"))
                                    nIndex_Start = dictionary["Start"];
                                if (dictionary.ContainsKey("End"))
                                    nIndex_End = dictionary["End"];
                                if (dictionary.ContainsKey("Count"))
                                    nIndex_Count = dictionary["Count"];
                                if (dictionary.ContainsKey("Proxy list"))
                                    nIndex_Proxylist = dictionary["Proxy list"];
                                bIsFirstline = false;
                            }
                            else
                            {
                                int nCreatedCount = 0;
                                if (!IsAdmin)
                                {
                                    DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                                    Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                                    foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                                    {
                                        if (trafficjob2.user_id == currentUserID)
                                            ++nCreatedCount;
                                    }
                                    if (nCreatedCount >= user.MaxJourneyCount)
                                        return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                                        {
                                            id = FolderId
                                        });
                                }
                                trafficjob trafficjob = new trafficjob();
                                trafficjob.user_id = currentUserID;
                                trafficjob.job_name = splits[nIndex_JobName];
                                trafficjob.journey_option = splits[nIndex_JourneyOption];
                                trafficjob.target_url = splits[nIndex_TargetURL];
                                trafficjob.exttarget_url = splits[nIndex_ExtTargetURL];
                                trafficjob.name_business = splits[nIndex_NameoftheBusiness];
                                trafficjob.search_term = splits[nIndex_SearchTerm].Replace("\"", "");
                                foreach (trafficjob.SEARCH_ENGINE enumValue in Enum.GetValues(typeof(trafficjob.SEARCH_ENGINE)))
                                {
                                    if (enumValue.GetAttribute<DisplayAttribute>().Name.Trim().Equals(splits[nIndex_SearchEngine].Trim(), StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        trafficjob.search_engine = enumValue;
                                        break;
                                    }
                                }
                                foreach (trafficjob.AGENT_KIND enumValue in Enum.GetValues(typeof(trafficjob.AGENT_KIND)))
                                {
                                    if (enumValue.GetAttribute<DisplayAttribute>().Name.Trim().Equals(splits[nIndex_AgentKind].Trim(), StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        trafficjob.agent_kind = enumValue;
                                        break;
                                    }
                                }
                                foreach (trafficjob.AGENT_AGE enumValue in Enum.GetValues(typeof(trafficjob.AGENT_AGE)))
                                {
                                    if (enumValue.GetAttribute<DisplayAttribute>().Name.Trim().Contains(splits[nIndex_Agentof].Trim()))
                                    {
                                        trafficjob.agent_age = enumValue;
                                        break;
                                    }
                                }
                                trafficjob.session_count = int.Parse(splits[nIndex_Count]);
                                trafficjob.group_id = FolderId;
                                if (trafficjob.session_count <= user.MaxRunCountPerDay)
                                {
                                    string str2 = trafficjob.journey_option.Replace(" with Google Form", "");
                                    if (!string.IsNullOrEmpty(user.JourneyOptionList))
                                    {
                                        string[] source = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                                        bool flag = false;
                                        for (int index = 0; index < ((IEnumerable<string>)source).Count<string>(); ++index)
                                        {
                                            if (str2 == source[index])
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (!flag)
                                            continue;
                                    }
                                    trafficjob.time_zone = splits[nIndex_TimeZone];
                                    trafficjob.start_time = splits[nIndex_Start].Replace(":00", "");
                                    trafficjob.end_time = splits[nIndex_End].Replace(":00", "");
                                    trafficjob.proxy_setting = splits[nIndex_Proxylist].Replace("null", "");
                                    if (trafficjob.proxy_setting != null && trafficjob.proxy_setting.Length > 500)
                                        trafficjob.proxy_setting = trafficjob.proxy_setting.Substring(0, 499);
                                    homeController._context.Add<trafficjob>(trafficjob);
                                    int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                                    homeController._context.Entry<trafficjob>(trafficjob).Reload();
                                    Startup.AddScheduleForToday(homeController._context, trafficjob.id);
                                    trafficjob = (trafficjob)null;
                                }
                                else
                                    continue;
                            }
                            splits = (List<string>)null;
                        }
                        filePath = (string)null;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = FolderId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder([Bind(new string[] { "id,job_name,journey_option,target_url,exttarget_url,search_term,search_engine,session_count, group_id" })] trafficjob trafficjob)
        {
            HomeController homeController = this;
            trafficjob.user_id = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            if (!homeController.ModelState.IsValid)
                return (IActionResult)homeController.View((object)trafficjob);
            homeController._context.Add<trafficjob>(trafficjob);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.RedirectToAction("Index");
        }

        public async Task<IActionResult> RemoveFolder(int id)
        {
            HomeController homeController = this;
            IEnumerable<trafficjob> listAsync = (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(homeController._context.trafficjob.Where<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == id || m.group_id == id)), new CancellationToken());
            homeController._context.trafficjob.RemoveRange(listAsync);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            return (IActionResult)homeController.RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            HomeController homeController = this;
            trafficjob trafficjob = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            if (trafficjob == null)
                return (IActionResult)homeController.NotFound();
            List<string> stringList = new List<string>();
            string path = Directory.GetCurrentDirectory() + "/country.csv";
            if (System.IO.File.Exists(path))
            {
                foreach (string readAllLine in System.IO.File.ReadAllLines(path))
                    stringList.Add(readAllLine);
            }
            this.ViewBag.countrycodelist = stringList;
            if (!string.IsNullOrEmpty(trafficjob.googlegeoresultselect))
            {
                this.ViewBag.count = trafficjob.googlegeoresultselect.Split(";", StringSplitOptions.None).Length;
                this.ViewBag.googleList = ((IEnumerable<string>)trafficjob.googlemaplist.Split(";", StringSplitOptions.None)).ToList<string>();
                this.ViewBag.googleLocation = ((IEnumerable<string>)trafficjob.googlegeoresultselect.Split(";", StringSplitOptions.None)).ToList<string>();
            }
            else
            {
                this.ViewBag.count = 0;
            }
            string str1 = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser byIdAsync = await homeController._userManager.FindByIdAsync(str1);
            bool flag1 = homeController.User.IsInRole("Admin");
            bool flag2 = false;
            if (!flag1)
            {
                if (byIdAsync.EnableGeoLocation)
                    flag2 = true;
            }
            else
                flag2 = true;
            homeController.ViewData["EnableGeoLocation"] = (object)flag2;
            List<SelectListItem> list1 = ((IEnumerable<string>)SettingManager.JoureyOptions).Select<string, SelectListItem>((Func<string, SelectListItem>)(x => new SelectListItem()
            {
                Text = x,
                Value = x
            })).ToList<SelectListItem>();
            List<SelectListItem> items = new List<SelectListItem>();
            if (!flag1)
            {
                bool scheduleInterupt = byIdAsync.EnableScheduleInterupt;
                if (string.IsNullOrEmpty(byIdAsync.JourneyOptionList))
                {
                    items = list1.ToList<SelectListItem>();
                }
                else
                {
                    string[] source = byIdAsync.JourneyOptionList.Split(';', StringSplitOptions.None);
                    foreach (SelectListItem selectListItem in list1)
                    {
                        bool flag3 = false;
                        string str2 = selectListItem.Text.Replace(" with Google Form", "");
                        for (int index = 0; index < ((IEnumerable<string>)source).Count<string>(); ++index)
                        {
                            if (str2 == source[index])
                            {
                                flag3 = true;
                                break;
                            }
                        }
                        if (flag3)
                            items.Add(selectListItem);
                    }
                }
            }
            else
                items = list1.ToList<SelectListItem>();
            this.ViewBag.JourneyOption = new SelectList((IEnumerable)items, "Value", "Text", (object)trafficjob.journey_option);
            List<Traffic.Models.location> locationList = new List<Traffic.Models.location>();
            List<Traffic.Models.location> list2 = homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>) (i => i.parentid == 0)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>) (o => o.label)).ToList<Traffic.Models.location>();
            this.ViewBag.List = list2;
            return (IActionResult)homeController.View((object)trafficjob);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(new string[] { "id, group_id, job_name, journey_option, target_url, exttarget_url, name_business, wild_card, search_term, snippet_content,location, search_engine, agent_kind, agent_age, session_count, proxy_setting, journey_url, form_fields, time_zone, start_time, end_time, googlemaplocal, googlemaplist,googlegeoresultselect,impressions,cookie_website" })] trafficjob newtrafficjob)
        {
            HomeController homeController = this;
            if (id != newtrafficjob.id)
                return (IActionResult)homeController.NotFound();
            trafficjob trafficjob = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            trafficjob.job_name = newtrafficjob.job_name;
            trafficjob.journey_option = newtrafficjob.journey_option;
            trafficjob.target_url = newtrafficjob.target_url;
            trafficjob.exttarget_url = newtrafficjob.exttarget_url;
            trafficjob.name_business = newtrafficjob.name_business;
            trafficjob.wild_card = newtrafficjob.wild_card;
            trafficjob.impressions = newtrafficjob.impressions;
            trafficjob.search_term = newtrafficjob.search_term;
            trafficjob.search_engine = newtrafficjob.search_engine;
            trafficjob.agent_kind = newtrafficjob.agent_kind;
            trafficjob.agent_age = newtrafficjob.agent_age;
            trafficjob.session_count = newtrafficjob.session_count;
            trafficjob.proxy_setting = newtrafficjob.proxy_setting;
            trafficjob.journey_url = newtrafficjob.journey_url;
            trafficjob.form_fields = newtrafficjob.form_fields;
            trafficjob.time_zone = newtrafficjob.time_zone;
            trafficjob.start_time = newtrafficjob.start_time;
            trafficjob.end_time = newtrafficjob.end_time;
            trafficjob.googlemaplocal = newtrafficjob.googlemaplocal;
            trafficjob.googlemaplist = newtrafficjob.googlemaplist;
            trafficjob.snippet_content = newtrafficjob.snippet_content;
            trafficjob.location = newtrafficjob.location;
            trafficjob.googlegeoresultselect = newtrafficjob.googlegeoresultselect;
            trafficjob.cookie_website = newtrafficjob.cookie_website;
            string source1 = "";
            if (trafficjob.search_term != null)
            {
                foreach (string str in trafficjob.search_term.Split("\n", StringSplitOptions.None))
                {
                    if (!string.IsNullOrEmpty(str))
                        source1 = source1 + str.Replace("\r", "") + "|";
                }
                if (source1.Length > 0 && source1.ElementAt<char>(source1.Length - 1) == '|')
                    source1 = source1.Substring(0, source1.Length - 1);
            }
            trafficjob.search_term = source1;
            string source2 = "";
            if (trafficjob.name_business != null)
            {
                foreach (string str in trafficjob.name_business.Split("\n", StringSplitOptions.None))
                {
                    if (!string.IsNullOrEmpty(str))
                        source2 = source2 + str.Replace("\r", "") + "|";
                }
                if (source2.Length > 0 && source2.ElementAt<char>(source2.Length - 1) == '|')
                    source2 = source2.Substring(0, source2.Length - 1);
            }
            trafficjob.name_business = source2;
            string currentUserID = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(currentUserID);
            if (!homeController.User.IsInRole("Admin"))
            {
                int nCreatedCount = 0;
                DbSet<trafficjob> trafficjob1 = homeController._context.trafficjob;
                Expression<Func<trafficjob, bool>> predicate = (Expression<Func<trafficjob, bool>>)(w => w.group_id != -1);
                foreach (trafficjob trafficjob2 in (IEnumerable<trafficjob>)await EntityFrameworkQueryableExtensions.ToListAsync<trafficjob>(trafficjob1.Where<trafficjob>(predicate), new CancellationToken()))
                {
                    if (trafficjob2.user_id == currentUserID)
                        ++nCreatedCount;
                }
                if (nCreatedCount >= user.MaxJourneyCount)
                    return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
                    {
                        id = trafficjob.group_id
                    });
                if (trafficjob.session_count > user.MaxRunCountPerDay)
                    trafficjob.session_count = user.MaxRunCountPerDay;
                string str = trafficjob.journey_option.Replace(" with Google Form", "");
                if (!string.IsNullOrEmpty(user.JourneyOptionList))
                {
                    string[] source3 = user.JourneyOptionList.Split(';', StringSplitOptions.None);
                    bool flag = false;
                    for (int index = 0; index < ((IEnumerable<string>)source3).Count<string>(); ++index)
                    {
                        if (str == source3[index])
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        return (IActionResult)homeController.RedirectToAction(nameof(Edit), (object)new
                        {
                            id = trafficjob.id
                        });
                }
            }
            trafficjob.user_id = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            if (!homeController.ModelState.IsValid)
                return (IActionResult)homeController.View((object)trafficjob);
            try
            {
                homeController._context.Update<trafficjob>(trafficjob);
                int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                Startup.EditScheduleForToday(homeController._context, trafficjob.id);
            }
            catch (Exception ex)
            {
            }
            return (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = trafficjob.group_id
            });
        }

        public async Task<IActionResult> Delete(int id)
        {
            HomeController homeController = this;
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            trafficjob model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            return model != null ? (IActionResult)homeController.View((object)model) : (IActionResult)homeController.NotFound();
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            HomeController homeController = this;
            trafficjob trafficjob = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            homeController._context.trafficjob.Remove(trafficjob);
            Startup.RemoveScheduleForToday(homeController._context, trafficjob.id);
            int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            IActionResult action = (IActionResult)homeController.RedirectToAction("Jobs", (object)new
            {
                id = trafficjob.group_id
            });
            trafficjob = (trafficjob)null;
            return action;
        }

        public async Task<IActionResult> LogView(int id, string startdate = "", string enddate = "")
        {
            HomeController homeController = this;
            List<log> logList = new List<log>();
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(str);
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            trafficjob trafficjob = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            if (trafficjob == null)
                return (IActionResult)homeController.NotFound();
            DateTime starttime = DateTime.MinValue;
            DateTime endtime = DateTime.MaxValue;
            if (startdate.Length > 0)
                starttime = Convert.ToDateTime(startdate);
            if (enddate.Length > 0)
                endtime = Convert.ToDateTime(enddate);
            List<log> listAsync = await EntityFrameworkQueryableExtensions.ToListAsync<log>(homeController._context.log.Where<log>((Expression<Func<log, bool>>)(w => w.job_id == id && starttime.Date <= w.record_time.Date && w.record_time.Date <= endtime.Date)), new CancellationToken());
            homeController.ViewData["job_name"] = (object)trafficjob.job_name;
            homeController.ViewData["groupid"] = (object)trafficjob.group_id;
            homeController.ViewData[nameof(id)] = (object)id;
            homeController.ViewData["type"] = (object)0;
            homeController.ViewData["starttime"] = (object)startdate;
            homeController.ViewData["endtime"] = (object)enddate;
            bool flag = homeController.User.IsInRole("Admin");
            if (!flag)
                flag = user.EnableSeeAdminLog;
            homeController.ViewData["showresult"] = (object)flag;
            return (IActionResult)homeController.View((object)listAsync);
        }

        public async Task<IActionResult> ResetJourney(int id)
        {
            HomeController homeController = this;
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            dailystate entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<dailystate>((IQueryable<dailystate>)homeController._context.dailystate, (Expression<Func<dailystate, bool>>)(m => m.id == id), new CancellationToken());
            homeController._context.Entry<dailystate>(entity).Reload();
            entity.state = 0;
            entity.consfailcount = 0;
            homeController._context.dailystate.Update(entity);
            homeController._context.SaveChanges();
            return (IActionResult)homeController.RedirectToAction("Statistics");
        }

        public async Task<IActionResult> RunNow(int id)
        {
            HomeController homeController = this;
            dailystate entity = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<dailystate>((IQueryable<dailystate>)homeController._context.dailystate, (Expression<Func<dailystate, bool>>)(m => m.id == id), new CancellationToken());
            if (entity == null)
                return (IActionResult)homeController.NotFound();
            entity.predict_time = DateTime.Now.Date;
            homeController._context.Update<dailystate>(entity);
            homeController._context.SaveChanges();
            return (IActionResult)homeController.RedirectToAction("Statistics");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogView(
          int id,
          string startdatepicker,
          string enddatepicker,
          int type)
        {
            HomeController homeController = this;
            List<log> logList = new List<log>();
            string str = homeController.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            ApplicationUser user = await homeController._userManager.FindByIdAsync(str);
            if (id == 0)
                return (IActionResult)homeController.NotFound();
            trafficjob trafficjob = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<trafficjob>((IQueryable<trafficjob>)homeController._context.trafficjob, (Expression<Func<trafficjob, bool>>)(m => m.id == id), new CancellationToken());
            if (trafficjob == null)
                return (IActionResult)homeController.NotFound();
            DateTime starttime = DateTime.MinValue;
            DateTime endtime = DateTime.MaxValue;
            if (startdatepicker.Length > 0)
                starttime = Convert.ToDateTime(startdatepicker);
            if (enddatepicker.Length > 0)
                endtime = Convert.ToDateTime(enddatepicker);
            List<log> listAsync = await EntityFrameworkQueryableExtensions.ToListAsync<log>(homeController._context.log.Where<log>((Expression<Func<log, bool>>)(w => w.job_id == id && starttime.Date <= w.record_time.Date && w.record_time.Date <= endtime.Date && (type == 0 || type == 1 && w.result == true || type == 2 && w.result == false))), new CancellationToken());
            homeController.ViewData["job_name"] = (object)trafficjob.job_name;
            homeController.ViewData["groupid"] = (object)trafficjob.group_id;
            homeController.ViewData[nameof(id)] = (object)id;
            homeController.ViewData[nameof(type)] = (object)type;
            homeController.ViewData["starttime"] = (object)startdatepicker;
            homeController.ViewData["endtime"] = (object)enddatepicker;
            bool flag = homeController.User.IsInRole("Admin");
            if (!flag)
                flag = user.EnableSeeAdminLog;
            homeController.ViewData["showresult"] = (object)flag;
            return (IActionResult)homeController.View((object)listAsync);
        }

        public async Task<IActionResult> HtmlBuilder(string id)
        {
            HomeController homeController = this;
            htmltemplate model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<htmltemplate>(homeController._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == Convert.ToInt32(id))), new CancellationToken());
            List<templates> templatesList = new List<templates>();
            List<templates> list1 = homeController._context.templates.ToList<templates>();
            this.ViewBag.listofitems = list1;
            List<Traffic.Models.location> locationList = new List<Traffic.Models.location>();
            List<Traffic.Models.location> list2 = homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>) (i => i.parentid == 0)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>) (o => o.label)).ToList<Traffic.Models.location>();
            this.ViewBag.List = list2;
            return (IActionResult)homeController.View((object)model);
        }

        public async Task<IActionResult> OverLink(string id)
        {
            HomeController homeController = this;
            overviewtool overviewtool = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<overviewtool>(homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.mainId == Convert.ToInt32(id))), new CancellationToken());
            if (overviewtool == null)
                overviewtool = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<overviewtool>(homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.id == Convert.ToInt32(id))), new CancellationToken());
            List<overviewtool> overviewtoolList = new List<overviewtool>();
            List<overviewtool> list = homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.parentId == Convert.ToInt32(id) && i.mainId == 0)).ToList<overviewtool>();
            this.ViewBag.URL = overviewtool.href;
            this.ViewBag.listofitems = homeController._context.overviewtool.ToList<overviewtool>();
            this.ViewBag.id = overviewtool.id;
            this.ViewBag.pid = id;
            this.ViewBag.mid = overviewtool.mainId;
            this.ViewBag.count = homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.parentId == overviewtool.id && i.mainId == 0)).ToList<overviewtool>().Count<overviewtool>();
            this.ViewBag.parentId = overviewtool.parentId;
            return (IActionResult)homeController.View((object)list);
        }

        public async Task<IActionResult> RssFeedView(string id)
        {
            HomeController homeController = this;
            try
            {
                rssfeedtool rssfeedtool = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<rssfeedtool>(homeController._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.id == Convert.ToInt32(id))), new CancellationToken());
                List<rssfeedtool> rssfeedtoolList = new List<rssfeedtool>();
                List<rssfeedtool> list = homeController._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.parentId == Convert.ToInt32(id))).ToList<rssfeedtool>();
                this.ViewBag.URL = rssfeedtool.href;
                this.ViewBag.listofitems = homeController._context.rssfeedtool.ToList<rssfeedtool>();
                this.ViewBag.id = rssfeedtool.id;
                return (IActionResult)homeController.View((object)list);
            }
            catch
            {
                return (IActionResult)null;
            }
        }

        public async Task<IActionResult> BuilderManager()
        {
            HomeController homeController = this;
            List<htmltemplate> htmltemplateList = new List<htmltemplate>();
            List<htmltemplate> list = homeController._context.htmltemplate.ToList<htmltemplate>();
            return (IActionResult)homeController.View((object)list);
        }

        public async Task<IActionResult> OverviewTool()
        {
            HomeController homeController = this;
            List<overviewtool> overviewtoolList = new List<overviewtool>();
            List<overviewtool> list = homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.parentId == 0)).ToList<overviewtool>();
            return (IActionResult)homeController.View((object)list);
        }

        public async Task<IActionResult> RssFeedTool()
        {
            HomeController homeController = this;
            List<rssfeedtool> rssfeedtoolList = new List<rssfeedtool>();
            List<rssfeedtool> list = homeController._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.parentId == 0)).ToList<rssfeedtool>();
            return (IActionResult)homeController.View((object)list);
        }

        public async Task<IActionResult> TwitterPost()
        {
            HomeController homeController = this;
            twitterpost model = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<twitterpost>((IQueryable<twitterpost>)homeController._context.twitterpost, new CancellationToken());
            return (IActionResult)homeController.View((object)model);
        }

        public static IWebDriver _driver
        {
            get
            {
                Thread.Sleep(500);
                return HomeController.driver;
            }
            set => HomeController.driver = value;
        }

        public async Task<IActionResult> twitterStart(string userslist, string urlslist)
        {
            HomeController homeController = this;
            twitterpost twitterpost = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<twitterpost>((IQueryable<twitterpost>)homeController._context.twitterpost, new CancellationToken());
            int num1 = 0;
            if (twitterpost == null)
            {
                twitterpost = new twitterpost();
                twitterpost.userlist = userslist;
                twitterpost.urls = urlslist;
                homeController._context.twitterpost.Add(twitterpost);
            }
            else
            {
                twitterpost.userlist = userslist;
                twitterpost.urls = urlslist;
                num1 = twitterpost.state;
                homeController._context.twitterpost.Update(twitterpost);
            }
            homeController._context.SaveChanges();
            if (num1 == 0)
            {
                if (userslist == "")
                    return (IActionResult)homeController.RedirectToAction("TwitterPost");
                string[] users = userslist.Split("\r\n", StringSplitOptions.None);
                if (urlslist == "")
                    return (IActionResult)homeController.RedirectToAction("TwitterPost");
                string[] urls = urlslist.Split("\r\n", StringSplitOptions.None);
                twitterpost = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<twitterpost>((IQueryable<twitterpost>)homeController._context.twitterpost, new CancellationToken());
                twitterpost.state = 1;
                homeController._context.twitterpost.Update(twitterpost);
                homeController._context.SaveChanges();
                int index1 = 0;
                string[] strArray = urls;
                for (int index2 = 0; index2 < strArray.Length; ++index2)
                {
                    string url = strArray[index2];
                    if ((await EntityFrameworkQueryableExtensions.ToListAsync<tweeturl>(homeController._context.tweeturl.Where<tweeturl>((Expression<Func<tweeturl, bool>>)(i => i.url == url)), new CancellationToken())).Count <= 0 && !(url == ""))
                    {
                        string[] strArray1 = users[index1 % users.Length].Split(":", StringSplitOptions.None);
                        string text1 = strArray1[0];
                        string text2 = strArray1[1];
                        string str1 = strArray1[2];
                        string str2 = strArray1[3];
                        string text3 = strArray1[4];
                        HtmlDocument htmlDocument = new HtmlDocument();
                        try
                        {
                            FirefoxOptions options = new FirefoxOptions()
                            {
                                Profile = new FirefoxProfile()
                                {
                                    DeleteAfterUse = true
                                }
                            };
                            string currentDirectory = Directory.GetCurrentDirectory();
                            string str3 = "us.smartproxy.io:10211";
                            options.Profile.SetProxyPreferences(new OpenQA.Selenium.Proxy()
                            {
                                HttpProxy = str3,
                                FtpProxy = str3,
                                SslProxy = str3
                            });
                            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                            HomeController._driver = (IWebDriver)new FirefoxDriver(currentDirectory, options);
                            int num2 = 30;
                            while (num2 > 0)
                            {
                                --num2;
                                HomeController._driver.Navigate().GoToUrl("https://api.ipify.org");
                                new WebDriverWait(HomeController._driver, TimeSpan.FromSeconds(30.0)).Until<IWebElement>(ExpectedConditions.ElementIsVisible(By.TagName("body")));
                                htmlDocument.LoadHtml(HomeController._driver.PageSource);
                                if (((IEnumerable<Match>)new Regex("\\b\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\b").Matches(htmlDocument.DocumentNode.SelectSingleNode("//body").InnerText)).Count<Match>() <= 0)
                                    Thread.Sleep(10000);
                                else
                                    break;
                            }
                            HomeController._driver.Navigate().GoToUrl("https://twitter.com/login");
                            IWebElement element1 = HomeController._driver.FindElement(By.ClassName("js-username-field"));
                            IWebElement element2 = HomeController._driver.FindElement(By.ClassName("js-password-field"));
                            element1.SendKeys(text1);
                            element2.SendKeys(text2);
                            HomeController._driver.FindElement(By.ClassName("signin")).FindElement(By.ClassName("EdgeButton")).Click();
                            if (HomeController._driver.PageSource.Contains("Edge-textbox"))
                            {
                                HomeController._driver.FindElement(By.ClassName("Edge-textbox")).SendKeys(text3);
                                HomeController._driver.FindElement(By.Id("email_challenge_submit")).Click();
                            }
                            Thread.Sleep(3000);
                            HomeController._driver.FindElement(By.ClassName("public-DraftStyleDefault-ltr")).Click();
                            Thread.Sleep(2000);
                            HomeController._driver.FindElement(By.XPath("//div[@data-testid='tweetTextarea_0']")).SendKeys(url);
                            HomeController._driver.FindElement(By.CssSelector("div[data-testid='tweetButtonInline']")).Click();
                            Thread.Sleep(3000);
                            MatchCollection matchCollection = Regex.Matches(HomeController._driver.PageSource, "title=\"(?<VAL>[^\\\"]*)\" href=\"(?<VAL1>[^\\\"]*)");
                            string str4 = "";
                            foreach (Match match in matchCollection)
                            {
                                if (match.Groups["VAL"].Value == url)
                                {
                                    str4 = match.Groups["VAL1"].Value;
                                    break;
                                }
                            }
                            homeController._context.tweeturl.Add(new tweeturl()
                            {
                                url = url,
                                turl = str4
                            });
                            homeController._context.SaveChanges();
                            twitterpost.urls = urlslist.Replace(url, "");
                            homeController._context.twitterpost.Update(twitterpost);
                            homeController._context.SaveChanges();
                            try
                            {
                                if (HomeController._driver != null)
                                {
                                    HomeController._driver.Close();
                                    HomeController._driver.Quit();
                                }
                            }
                            catch
                            {
                            }
                        }
                        catch (Exception ex)
                        {
                            if (HomeController._driver != null)
                            {
                                HomeController._driver.Close();
                                HomeController._driver.Quit();
                            }
                        }
                        ++index1;
                    }
                }
                strArray = (string[])null;
                twitterpost = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<twitterpost>((IQueryable<twitterpost>)homeController._context.twitterpost, new CancellationToken());
                twitterpost.state = 0;
                homeController._context.twitterpost.Update(twitterpost);
                homeController._context.SaveChanges();
                users = (string[])null;
                urls = (string[])null;
            }
            return (IActionResult)homeController.RedirectToAction("TwitterPost");
        }

        public JsonResult CreateHtml(string jobname)
        {
            try
            {
                htmltemplate htmltemplate = new htmltemplate();
                string data;
                if (this._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.jobname == jobname)).SingleOrDefault<htmltemplate>() == null)
                {
                    this._context.htmltemplate.Add(new htmltemplate()
                    {
                        jobname = jobname,
                        totalCnt = 0,
                        processCnt = 0
                    });
                    this._context.SaveChanges();
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/wwwroot/files/" + jobname.Replace(" ", "-"));
                    data = "{\"data\":\"success\"}";
                }
                else
                    data = "{\"data\":\"already\"}";
                return this.Json((object)data);
            }
            catch (Exception ex)
            {
                return this.Json((object)"{\"data\":\"failed\"}");
            }
        }

        public JsonResult CreateToolProject(
          string projectname,
          string href,
          int parentid,
          string anchor = "")
        {
            try
            {
                overviewtool overviewtool = new overviewtool();
                string data;
                if (projectname != null)
                {
                    if (this._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.projectname == projectname)).SingleOrDefault<overviewtool>() == null)
                    {
                        overviewtool entity = new overviewtool();
                        entity.projectname = projectname;
                        entity.parentId = parentid;
                        entity.anchor = anchor;
                        this._context.overviewtool.Add(entity);
                        this._context.SaveChanges();
                        int id = entity.id;
                        if (parentid == 0)
                        {
                            this._context.overviewtool.Add(new overviewtool()
                            {
                                parentId = id,
                                mainId = id,
                                href = href
                            });
                            this._context.SaveChanges();
                        }
                        data = "{\"data\":\"success\"}";
                    }
                    else
                        data = "{\"data\":\"already\"}";
                }
                else
                {
                    this._context.overviewtool.Add(new overviewtool()
                    {
                        projectname = "",
                        parentId = parentid,
                        href = href,
                        anchor = anchor
                    });
                    this._context.SaveChanges();
                    data = "{\"data\":\"success\"}";
                }
                return this.Json((object)data);
            }
            catch (Exception ex)
            {
                return this.Json((object)"{\"data\":\"failed\"}");
            }
        }

        public JsonResult CreateRssProject(string href, int parentid)
        {
            string data = "";
            try
            {
                rssfeedtool rssfeedtool = new rssfeedtool();
                if (href != null)
                {
                    if (this._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.href == href)).SingleOrDefault<rssfeedtool>() == null)
                    {
                        this._context.rssfeedtool.Add(new rssfeedtool()
                        {
                            parentId = parentid,
                            href = href
                        });
                        this._context.SaveChanges();
                        data = "{\"data\":\"success\"}";
                    }
                    else
                        data = "{\"data\":\"already\"}";
                }
                return this.Json((object)data);
            }
            catch (Exception ex)
            {
                return this.Json((object)"{\"data\":\"failed\"}");
            }
        }

        public JsonResult ScrapUrl(string mainUrl, int parentid)
        {
            try
            {
                HttpResponseMessage result1 = new HttpClient().GetAsync(mainUrl).Result;
                result1.EnsureSuccessStatusCode();
                string result2 = result1.Content.ReadAsStringAsync().Result;
                string str1 = Regex.Match(result2, "title>(?<VAL>[^\\<]*)").Groups["VAL"].Value;
                MatchCollection matchCollection = Regex.Matches(result2, "href=\"(?<url>[^\\\"]*)");
                List<string> source = new List<string>();
                string str2 = mainUrl.Split(new string[1] { "://" }, 2, StringSplitOptions.None)[1];
                foreach (Match match in matchCollection)
                {
                    if (match.Groups["url"].Value.Contains("post"))
                    {
                        if (match.Groups["url"].Value.StartsWith("/"))
                        {
                            if (match.Groups["url"].Value.EndsWith("/"))
                                source.Add(mainUrl + match.Groups["url"].Value.Substring(1, match.Groups["url"].Value.Length - 1));
                            else
                                source.Add(mainUrl + match.Groups["url"].Value);
                        }
                        else if (match.Groups["url"].Value.Contains(str2))
                            source.Add(match.Groups["url"].Value);
                    }
                }
                List<string> list = source.Distinct<string>().ToList<string>();
                List<string> stringList = new List<string>();
                foreach (string str3 in list)
                {
                    string href = str3;
                    rssfeedtool rssfeedtool = new rssfeedtool();
                    if (this._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.href == href)).SingleOrDefault<rssfeedtool>() == null)
                    {
                        rssfeedtool entity = new rssfeedtool();
                        entity.parentId = parentid;
                        entity.href = href;
                        entity.rss = this.getRssFeed(href);
                        stringList.Add(entity.rss);
                        this._context.rssfeedtool.Add(entity);
                        this._context.SaveChanges();
                    }
                }
                rssfeedtool rssfeedtool1 = new rssfeedtool();
                rssfeedtool entity1 = this._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.id == parentid)).SingleOrDefault<rssfeedtool>();
                string str4 = "<rss version=\"2.0\"><channel>" + "<title>" + str1 + "</title>";
                foreach (string str5 in stringList)
                    str4 += str5.Replace("<rss version=\"2.0\">", "").Replace("</rss>", "").Replace("channel>", "item>");
                string str6 = str4 + "</channel></rss>";
                entity1.rss = str6;
                this._context.rssfeedtool.Update(entity1);
                this._context.SaveChanges();
                return this.Json((object)"{\"data\":\"success\"}");
            }
            catch (Exception ex)
            {
                return this.Json((object)"{\"data\":\"failed\"}");
            }
        }

        public string getRssFeed(string href)
        {
            try
            {
                string str1 = "<rss version=\"2.0\"><channel>";
                HttpResponseMessage result1 = new HttpClient().GetAsync(href).Result;
                result1.EnsureSuccessStatusCode();
                string result2 = result1.Content.ReadAsStringAsync().Result;
                string str2 = Regex.Match(result2, "title>(?<VAL>[^\\<]*)").Groups["VAL"].Value;
                string str3 = str1 + "<title>" + str2 + "</title><link>" + href + "</link>";
                string str4 = Regex.Match(result2, "img src=\"(?<VAL>[^\\\"]*)").Groups["VAL"].Value;
                if (str4 != "")
                    str3 = str3 + "<image><url>" + str4 + "</url></image>";
                List<string> source = new List<string>();
                foreach (Match match in Regex.Matches(result2, "<p[^\\>]*>(?<VAL>[^\\<]*)</p>"))
                    source.Add(match.Groups["VAL"].Value);
                foreach (Match match in Regex.Matches(result2, "<h[^\\>]*>(?<VAL>[^\\<]*)</h"))
                    source.Add(match.Groups["VAL"].Value);
                foreach (Match match in Regex.Matches(result2, "<div [^\\>]*>(?<VAL>[^\\<]*)</div>"))
                    source.Add(match.Groups["VAL"].Value);
                List<string> list = source.Distinct<string>().ToList<string>();
                if (list.Count > 0)
                {
                    string str5 = str3 + "<description>";
                    foreach (string str6 in list)
                        str5 += str6;
                    str3 = str5 + "</description>";
                }
                return str3 + "</channel></rss>";
            }
            catch
            {
                return "";
            }
        }

        public async Task<IActionResult> DeleteToolProject(string id)
        {
            HomeController homeController = this;
            try
            {
                overviewtool overviewtool = new overviewtool();
                overviewtool entity = homeController._context.overviewtool.Where<overviewtool>((Expression<Func<overviewtool, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<overviewtool>();
                string projectname = entity.projectname;
                homeController._context.overviewtool.Remove(entity);
                homeController._context.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            return (IActionResult)homeController.RedirectToAction("OverviewTool");
        }

        public async Task<IActionResult> DeleteRssTool(string id)
        {
            HomeController homeController = this;
            int num = 0;
            try
            {
                rssfeedtool rssfeedtool = new rssfeedtool();
                num = homeController._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<rssfeedtool>().parentId;
                rssfeedtool entity = homeController._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<rssfeedtool>();
                homeController._context.rssfeedtool.Remove(entity);
                homeController._context.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            id = num.ToString();
            return !(id == "0") ? (IActionResult)homeController.RedirectToAction("RssFeedView", (object)new
            {
                id = id
            }) : (IActionResult)homeController.RedirectToAction("RssFeedTool");
        }

        public async Task<IActionResult> DeleteHtml(string id)
        {
            HomeController homeController = this;
            try
            {
                htmltemplate htmltemplate = new htmltemplate();
                htmltemplate entity = homeController._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<htmltemplate>();
                string jobname = entity.jobname;
                homeController._context.htmltemplate.Remove(entity);
                homeController._context.SaveChanges();
                foreach (FileSystemInfo file in new DirectoryInfo(Directory.GetCurrentDirectory() + "/wwwroot/files/" + jobname).GetFiles())
                    file.Delete();
                Directory.Delete(Directory.GetCurrentDirectory() + "/wwwroot/files/" + jobname);
            }
            catch (Exception ex)
            {
            }
            return (IActionResult)homeController.RedirectToAction("BuilderManager");
        }

        [HttpPost]
        public string GetRssXml(string id)
        {
            try
            {
                return this._context.rssfeedtool.Where<rssfeedtool>((Expression<Func<rssfeedtool, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<rssfeedtool>().rss;
            }
            catch
            {
                return "";
            }
        }

        [HttpPost]
        public JsonResult changetemp(int id) => this.Json((object)WebUtility.UrlEncode(this._context.templates.Find((object)id).html));

        public async Task<IActionResult> location()
        {
            HomeController homeController = this;
            IEnumerable<Traffic.Models.location> listAsync = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            List<Traffic.Models.location> locationList = new List<Traffic.Models.location>();
            foreach (Traffic.Models.location location in listAsync)
            {
                if (location.depth == 1)
                    locationList.Add(location);
            }
            this.ViewBag.listofitems = locationList;
            return (IActionResult)homeController.View();
        }

        public async Task<IActionResult> template()
        {
            HomeController homeController = this;
            List<templates> templatesList = new List<templates>();
            List<templates> list = homeController._context.templates.ToList<templates>();
            this.ViewBag.listofitems = list;
            return (IActionResult)homeController.View();
        }

        [HttpPost]
        public async Task<JsonResult> addCountry(string name)
        {
            HomeController homeController = this;
            try
            {
                string result = "{\"data\":[";
                string[] strArray = name.Split('\n', StringSplitOptions.None);
                for (int index = 0; index < strArray.Length; ++index)
                {
                    string str = strArray[index];
                    Traffic.Models.location newlocation = new Traffic.Models.location();
                    newlocation.parentid = 0;
                    newlocation.depth = 1;
                    newlocation.label = str;
                    homeController._context.location.Add(newlocation);
                    int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    result = result + "{\"id\":" + newlocation.id.ToString() + ",\"name\":\"" + newlocation.label + "\"},";
                    newlocation = (Traffic.Models.location)null;
                }
                strArray = (string[])null;
                result = result.Substring(0, result.Length - 1);
                result += "]}";
                return homeController.Json((object)result);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> dellocation(int id)
        {
            HomeController homeController = this;
            try
            {
                homeController._context.location.Remove(homeController._context.location.Find((object)id));
                int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                string data = "{\"data\":\"success\"}";
                return homeController.Json((object)data);
            }
            catch
            {
                string data = "{\"data\":\"failed\"}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> addState(int id, string name, string shortname)
        {
            HomeController homeController = this;
            try
            {
                string result = "{\"data\":[";
                string[] strArray1 = name.Split('\n', StringSplitOptions.None);
                string[] shortnames = shortname.Split('\n', StringSplitOptions.None);
                int i = 0;
                string[] strArray = strArray1;
                for (int index = 0; index < strArray.Length; ++index)
                {
                    string str = strArray[index];
                    Traffic.Models.location newlocation = new Traffic.Models.location();
                    newlocation.parentid = id;
                    newlocation.depth = 2;
                    newlocation.label = str;
                    newlocation.shortlabel = shortnames[i];
                    homeController._context.location.Add(newlocation);
                    int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    result = result + "{\"id\":" + newlocation.id.ToString() + ",\"name\":\"" + newlocation.label + "\",\"shortname\":\"" + newlocation.shortlabel + "\"},";
                    ++i;
                    newlocation = (Traffic.Models.location)null;
                }
                strArray = (string[])null;
                result = result.Substring(0, result.Length - 1);
                result += "]}";
                return homeController.Json((object)result);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> addCity(int id, string name)
        {
            HomeController homeController = this;
            try
            {
                string result = "{\"data\":[";
                string[] strArray = name.Split('\n', StringSplitOptions.None);
                for (int index = 0; index < strArray.Length; ++index)
                {
                    string str = strArray[index];
                    Traffic.Models.location newlocation = new Traffic.Models.location();
                    newlocation.parentid = id;
                    newlocation.depth = 3;
                    newlocation.label = str;
                    homeController._context.location.Add(newlocation);
                    int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    result = result + "{\"id\":" + newlocation.id.ToString() + ",\"name\":\"" + newlocation.label + "\"},";
                    newlocation = (Traffic.Models.location)null;
                }
                strArray = (string[])null;
                result = result.Substring(0, result.Length - 1);
                result += "]}";
                return homeController.Json((object)result);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> addNeighbor(int id, string name)
        {
            HomeController homeController = this;
            try
            {
                string result = "{\"data\":[";
                string[] strArray = name.Split('\n', StringSplitOptions.None);
                for (int index = 0; index < strArray.Length; ++index)
                {
                    string str = strArray[index];
                    Traffic.Models.location newlocation = new Traffic.Models.location();
                    newlocation.parentid = id;
                    newlocation.depth = 4;
                    newlocation.label = str;
                    homeController._context.location.Add(newlocation);
                    int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    result = result + "{\"id\":" + newlocation.id.ToString() + ",\"name\":\"" + newlocation.label + "\"},";
                    newlocation = (Traffic.Models.location)null;
                }
                strArray = (string[])null;
                result = result.Substring(0, result.Length - 1);
                result += "]}";
                return homeController.Json((object)result);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> changestate(int id)
        {
            HomeController homeController = this;
            IEnumerable<Traffic.Models.location> listAsync1 = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 2 && i.parentid == id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            int flag = 0;
            string result = "";
            int cityid = 0;
            int stateid = 0;
            if (listAsync1.Count<Traffic.Models.location>() > 0)
                result += "{\"state\":[";
            foreach (Traffic.Models.location location in listAsync1)
            {
                if (flag == 0)
                {
                    stateid = location.id;
                    flag = 1;
                }
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + " - " + location.shortlabel + "\"},";
            }
            if (listAsync1.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            flag = 0;
            IEnumerable<Traffic.Models.location> listAsync2 = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 3 && i.parentid == stateid)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            if (listAsync2.Count<Traffic.Models.location>() > 0)
                result += ",\"city\":[";
            foreach (Traffic.Models.location location in listAsync2)
            {
                if (flag == 0)
                {
                    cityid = location.id;
                    flag = 1;
                }
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            }
            if (listAsync2.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            flag = 0;
            IEnumerable<Traffic.Models.location> listAsync3 = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 4 && i.parentid == cityid)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            if (listAsync3.Count<Traffic.Models.location>() > 0)
                result += ",\"neighbor\":[";
            foreach (Traffic.Models.location location in listAsync3)
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            if (listAsync3.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            result += "}";
            JsonResult jsonResult = homeController.Json((object)result);
            result = (string)null;
            return jsonResult;
        }

        [HttpPost]
        public async Task<JsonResult> changeCity(int id)
        {
            HomeController homeController = this;
            int flag = 0;
            string result = "";
            int cityid = 0;
            IEnumerable<Traffic.Models.location> listAsync1 = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 3 && i.parentid == id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            if (listAsync1.Count<Traffic.Models.location>() > 0)
                result += "{\"city\":[";
            foreach (Traffic.Models.location location in listAsync1)
            {
                if (flag == 0)
                {
                    cityid = location.id;
                    flag = 1;
                }
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            }
            if (listAsync1.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            IEnumerable<Traffic.Models.location> listAsync2 = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 4 && i.parentid == cityid)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            if (listAsync2.Count<Traffic.Models.location>() > 0)
                result += ",\"neighbor\":[";
            foreach (Traffic.Models.location location in listAsync2)
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            if (listAsync2.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            result += "}";
            JsonResult jsonResult = homeController.Json((object)result);
            result = (string)null;
            return jsonResult;
        }

        [HttpPost]
        public async Task<JsonResult> changeCity1(int[] ids)
        {
            HomeController homeController = this;
            string result = "";
            result += "{\"city\":[";
            int[] numArray = ids;
            for (int index = 0; index < numArray.Length; ++index)
            {
                int id = numArray[index];
                IQueryable<Traffic.Models.location> source = homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 3 && i.parentid == id));
                Expression<Func<Traffic.Models.location, string>> keySelector = (Expression<Func<Traffic.Models.location, string>>)(o => o.label);
                foreach (Traffic.Models.location location in (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)source.OrderBy<Traffic.Models.location, string>(keySelector), new CancellationToken()))
                    result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            }
            numArray = (int[])null;
            result = result.Substring(0, result.Length - 1);
            result += "]}";
            JsonResult jsonResult = homeController.Json((object)result);
            result = (string)null;
            return jsonResult;
        }

        [HttpPost]
        public async Task<JsonResult> changeNeighborhood(int id)
        {
            HomeController homeController = this;
            string result = "";
            IEnumerable<Traffic.Models.location> listAsync = (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 4 && i.parentid == id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken());
            if (listAsync.Count<Traffic.Models.location>() > 0)
                result += "{\"neighbor\":[";
            foreach (Traffic.Models.location location in listAsync)
                result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            if (listAsync.Count<Traffic.Models.location>() > 0)
            {
                result = result.Substring(0, result.Length - 1);
                result += "]";
            }
            result += "}";
            JsonResult jsonResult = homeController.Json((object)result);
            result = (string)null;
            return jsonResult;
        }

        [HttpPost]
        public async Task<JsonResult> changeNeighborhood1(int[] ids)
        {
            HomeController homeController = this;
            string result = "";
            result += "{\"neighbor\":[";
            int[] numArray = ids;
            for (int index = 0; index < numArray.Length; ++index)
            {
                int id = numArray[index];
                IOrderedQueryable<Traffic.Models.location> source = homeController._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 4 && i.parentid == id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label));
                Expression<Func<Traffic.Models.location, string>> keySelector = (Expression<Func<Traffic.Models.location, string>>)(o => o.label);
                foreach (Traffic.Models.location location in (IEnumerable<Traffic.Models.location>)await EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)source.OrderBy<Traffic.Models.location, string>(keySelector), new CancellationToken()))
                    result = result + "{\"id\":" + location.id.ToString() + ",\"name\":\"" + location.label + "\"},";
            }
            numArray = (int[])null;
            result = result.Substring(0, result.Length - 1);
            result += "]}";
            JsonResult jsonResult = homeController.Json((object)result);
            result = (string)null;
            return jsonResult;
        }

        [HttpPost]
        public async Task<JsonResult> AddLocation(int type, string ids, int rid)
        {
            HomeController homeController = this;
            try
            {
                string result = "";
                string[] strArray = ids.Split(',', StringSplitOptions.None);
                string str1 = "";
                List<int> source = new List<int>();
                foreach (string str2 in strArray)
                {
                    source.Add(Convert.ToInt32(str2));
                    Traffic.Models.location location1 = homeController._context.location.Find((object)Convert.ToInt32(str2));
                    if (location1.depth == type + 1)
                    {
                        str1 = location1.label;
                        int parentid = location1.parentid;
                        source.Add(parentid);
                        while (parentid != 0)
                        {
                            Traffic.Models.location location2 = homeController._context.location.Find((object)parentid);
                            str1 = str1 + ", " + location2.label;
                            parentid = location2.parentid;
                            source.Add(parentid);
                            if (location2.depth == 1)
                            {
                                str1 += "\r\n";
                                break;
                            }
                        }
                    }
                    result += str1;
                }
                List<int> list = source.Distinct<int>().ToList<int>();
                ids = "";
                foreach (int num in list)
                {
                    if (num != 0)
                        ids = ids + num.ToString() + ",";
                }
                ids = ids.Substring(0, ids.Length - 1);
                htmltemplate entity1 = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<htmltemplate>(homeController._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == rid)), new CancellationToken());
                if (entity1 == null)
                {
                    entity1.type = type;
                    entity1.location = ids;
                    entity1.loc_str = result;
                    htmltemplate entity2 = new htmltemplate();
                    homeController._context.htmltemplate.Add(entity2);
                }
                else
                {
                    entity1.type = type;
                    entity1.location = ids;
                    entity1.loc_str = result;
                    homeController._context.htmltemplate.Update(entity1);
                }
                homeController._context.SaveChanges();
                return homeController.Json((object)result);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckLocation(int type, string ids)
        {
            HomeController homeController = this;
            try
            {
                string data = "";
                string[] strArray = ids.Split(',', StringSplitOptions.None);
                string str1 = "";
                List<int> intList = new List<int>();
                foreach (string str2 in strArray)
                {
                    intList.Add(Convert.ToInt32(str2));
                    Traffic.Models.location location1 = homeController._context.location.Find((object)Convert.ToInt32(str2));
                    if (location1.depth == type + 1)
                    {
                        str1 = location1.label;
                        int parentid = location1.parentid;
                        intList.Add(parentid);
                        while (parentid != 0)
                        {
                            Traffic.Models.location location2 = homeController._context.location.Find((object)parentid);
                            str1 = str1 + ", " + location2.label;
                            parentid = location2.parentid;
                            intList.Add(parentid);
                            if (location2.depth == 1)
                            {
                                str1 += "\r\n";
                                break;
                            }
                        }
                    }
                    data += str1;
                }
                return homeController.Json((object)data);
            }
            catch
            {
                string data = "{'data':'failed'}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HtmlBuilder(
          string htmlcontent,
          string keyword,
          string content,
          string keywordlist,
          bool selLoc,
          string id)
        {
            try
            {
                htmltemplate entity1 = this._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<htmltemplate>();
                string str1 = "";
                int num1 = 0;
                string str2 = "";
                if (entity1 == null)
                {
                    entity1 = new htmltemplate();
                    entity1.html = htmlcontent;
                    entity1.keyword = keyword;
                    entity1.content = content;
                    entity1.keywordlist = keywordlist;
                    entity1.state = 1;
                    this._context.htmltemplate.Add(entity1);
                }
                else
                {
                    entity1.html = htmlcontent;
                    entity1.keyword = keyword;
                    entity1.content = content;
                    entity1.keywordlist = keywordlist;
                    entity1.state = 1;
                    str1 = entity1.location;
                    num1 = entity1.type;
                    str2 = entity1.jobname;
                    this._context.htmltemplate.Update(entity1);
                }
                this._context.SaveChanges();
                List<int> id_list = new List<int>();
                if (selLoc)
                {
                    foreach (string str3 in str1.Split(',', StringSplitOptions.None))
                        id_list.Add(Convert.ToInt32(str3));
                }
                List<string> stringList1 = new List<string>();
                MatchCollection matchCollection = Regex.Matches(content, "{(?<VAL>[^\\}]*)}");
                List<string>[] source1 = new List<string>[matchCollection.Count];
                int index1 = 0;
                foreach (Match match in matchCollection)
                {
                    source1[index1] = new List<string>();
                    string str4 = match.Groups["VAL"].Value;
                    if (str4.Contains('|'))
                    {
                        foreach (string str5 in str4.Split('|', StringSplitOptions.None))
                            source1[index1].Add(str5);
                    }
                    ++index1;
                }
                List<string> stringList2 = new List<string>();
                for (int index2 = 0; index2 < ((IEnumerable<List<string>>)source1).Count<List<string>>(); ++index2)
                {
                    if (index2 + 1 <= ((IEnumerable<List<string>>)source1).Count<List<string>>())
                    {
                        stringList2.Clear();
                        for (int index3 = 0; index3 < source1[index2].Count; ++index3)
                        {
                            if (index2 == 0)
                            {
                                stringList2.Add(source1[index2][index3]);
                            }
                            else
                            {
                                for (int index4 = 0; index4 < stringList1.Count; ++index4)
                                    stringList2.Add(stringList1[index4] + " " + source1[index2][index3]);
                            }
                        }
                        stringList1.Clear();
                        for (int index5 = 0; index5 < stringList2.Count; ++index5)
                            stringList1.Add(stringList2[index5]);
                    }
                }
                Traffic.Models.location location = new Traffic.Models.location();
                if (selLoc)
                    location = this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.id == id_list[0])).SingleOrDefault<Traffic.Models.location>();
                else
                    location = this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.label == "United States of America")).SingleOrDefault<Traffic.Models.location>();
                IEnumerable<Traffic.Models.location> result1 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == location.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                List<string> stringList3 = new List<string>();
                List<string>[] stringListArray1 = new List<string>[EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>(this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 3)), new CancellationToken()).Result.Count<Traffic.Models.location>()];
                Dictionary<string, List<string>> dictionary1 = new Dictionary<string, List<string>>();
                Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
                List<string>[] stringListArray2 = new List<string>[EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>(this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.depth == 4)), new CancellationToken()).Result.Count<Traffic.Models.location>()];
                int index6 = 0;
                int index7 = 0;
                foreach (Traffic.Models.location location1 in result1)
                {
                    Traffic.Models.location locationitr = location1;
                    if (selLoc && num1 > 0)
                    {
                        if (id_list.Contains(locationitr.id))
                        {
                            stringList3.Add(locationitr.label);
                            IEnumerable<Traffic.Models.location> result2 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == locationitr.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                            if (result2.Count<Traffic.Models.location>() > 0)
                            {
                                stringListArray1[index6] = new List<string>();
                                foreach (Traffic.Models.location location2 in result2)
                                {
                                    Traffic.Models.location lc = location2;
                                    if (num1 > 1)
                                    {
                                        if (id_list.Contains(lc.id))
                                        {
                                            stringListArray1[index6].Add(lc.label);
                                            IEnumerable<Traffic.Models.location> result3 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == lc.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                                            if (result3.Count<Traffic.Models.location>() > 0)
                                            {
                                                stringListArray2[index7] = new List<string>();
                                                foreach (Traffic.Models.location location3 in result3)
                                                {
                                                    if (num1 == 3)
                                                    {
                                                        if (id_list.Contains(lc.id))
                                                            stringListArray2[index7].Add(location3.label);
                                                    }
                                                    else
                                                        stringListArray2[index7].Add(location3.label);
                                                }
                                                dictionary2.Add(lc.label, stringListArray2[index7]);
                                                ++index7;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        stringListArray1[index6].Add(lc.label);
                                        IEnumerable<Traffic.Models.location> result4 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == lc.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                                        if (result4.Count<Traffic.Models.location>() > 0)
                                        {
                                            stringListArray2[index7] = new List<string>();
                                            foreach (Traffic.Models.location location4 in result4)
                                                stringListArray2[index7].Add(location4.label);
                                            dictionary2.Add(lc.label, stringListArray2[index7]);
                                            ++index7;
                                        }
                                    }
                                }
                                dictionary1.Add(locationitr.label, stringListArray1[index6]);
                                ++index6;
                            }
                        }
                    }
                    else
                    {
                        stringList3.Add(locationitr.label);
                        IEnumerable<Traffic.Models.location> result5 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == locationitr.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                        if (result5.Count<Traffic.Models.location>() > 0)
                        {
                            stringListArray1[index6] = new List<string>();
                            foreach (Traffic.Models.location location5 in result5)
                            {
                                Traffic.Models.location lc = location5;
                                stringListArray1[index6].Add(lc.label);
                                IEnumerable<Traffic.Models.location> result6 = (IEnumerable<Traffic.Models.location>)EntityFrameworkQueryableExtensions.ToListAsync<Traffic.Models.location>((IQueryable<Traffic.Models.location>)this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.parentid == lc.id)).OrderBy<Traffic.Models.location, string>((Expression<Func<Traffic.Models.location, string>>)(o => o.label)), new CancellationToken()).Result;
                                if (result6.Count<Traffic.Models.location>() > 0)
                                {
                                    stringListArray2[index7] = new List<string>();
                                    foreach (Traffic.Models.location location6 in result6)
                                        stringListArray2[index7].Add(location6.label);
                                    dictionary2.Add(lc.label, stringListArray2[index7]);
                                    ++index7;
                                }
                            }
                            dictionary1.Add(locationitr.label, stringListArray1[index6]);
                            ++index6;
                        }
                    }
                }
                Random random = new Random();
                foreach (FileSystemInfo file in new DirectoryInfo(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-")).GetFiles())
                    file.Delete();
                string str6 = "<!DOCTYPE html>" + Environment.NewLine + "<html>" + Environment.NewLine + "<head>" + Environment.NewLine + "<title>Html Builder</title>" + Environment.NewLine + "</head>" + Environment.NewLine + "<body>" + Environment.NewLine + "</body>" + Environment.NewLine + "</html>";
                int num2 = str6.IndexOf("<head>") + 6;
                string[] strArray = keyword.Split("\r\n", StringSplitOptions.None);
                string[] source2 = keywordlist.Split("\r\n", StringSplitOptions.None);
                int num3 = 0;
                foreach (string input in strArray)
                {
                    string str7 = Regex.Replace(input, "\\s+", "-");
                    int startIndex1 = str6.IndexOf("</body>");
                    string str8 = "<p><a href=\"/files/" + str2.Replace(" ", "-") + "/" + str7 + ".html\" target=\"_blank\">" + input + "</a></p>" + Environment.NewLine;
                    str6 = str6.Insert(startIndex1, str8);
                    using (StreamWriter streamWriter1 = System.IO.File.AppendText(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/" + str7 + ".html"))
                    {
                        string str9 = "<!DOCTYPE html>" + Environment.NewLine + "<html>" + Environment.NewLine + "<head>" + Environment.NewLine + "<title>" + input + " service area</title>" + Environment.NewLine + "</head>" + Environment.NewLine + "<body>" + Environment.NewLine + "</body>" + Environment.NewLine + "</html>";
                        num2 = str9.IndexOf("<head>") + 6;
                        int startIndex2 = str9.IndexOf("<body>") + 6;
                        string str10 = input + " service area";
                        string str11 = str9.Insert(startIndex2, str10);
                        foreach (string str12 in stringList3)
                        {
                            string state = str12;
                            string str13 = Regex.Replace(state, "\\s+", "-").Replace("/", "-");
                            int startIndex3 = str11.IndexOf("</body>");
                            string shortlabel = this._context.location.Where<Traffic.Models.location>((Expression<Func<Traffic.Models.location, bool>>)(i => i.label == state && i.depth == 2)).Single<Traffic.Models.location>().shortlabel;
                            string str14 = "<p><a href=\"/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str13.ToLower() + ".html\" target=\"_blank\">" + state + "</a></p>" + Environment.NewLine;
                            str11 = str11.Insert(startIndex3, str14);
                            using (StreamWriter streamWriter2 = System.IO.File.AppendText(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str13.ToLower() + ".html"))
                            {
                                string html1 = entity1.html;
                                num2 = html1.IndexOf("<head>") + 6;
                                string newValue1 = input + " " + state;
                                int startIndex4 = html1.IndexOf("<body>") + 6;
                                string str15 = "<header id='header' class='hoc clear'>" + input + " " + state + "</header>";
                                string str16 = !html1.Contains("$title") ? html1.Insert(startIndex4, str15) : html1.Replace("$title", newValue1);
                                string newValue2 = "";
                                if (dictionary1.ContainsKey(state))
                                {
                                    foreach (string str17 in dictionary1[state])
                                    {
                                        string str18 = Regex.Replace(str17, "\\s+", "-").Replace("/", "-");
                                        int num4 = str16.IndexOf("</body>");
                                        string str19 = "<p><a href=\"/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str18.ToLower() + "-" + shortlabel.ToLower() + ".html\" target=\"_blank\">" + str17 + "</a></p>" + Environment.NewLine;
                                        newValue2 += str19;
                                        using (StreamWriter streamWriter3 = System.IO.File.AppendText(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str18.ToLower() + "-" + shortlabel.ToLower() + ".html"))
                                        {
                                            string html2 = entity1.html;
                                            num2 = html2.IndexOf("<head>") + 6;
                                            string newValue3 = input + " " + str17 + " " + shortlabel;
                                            int startIndex5 = html2.IndexOf("<body>") + 6;
                                            string str20 = "<header id='header' class='hoc clear'>" + input + " " + str17 + " " + shortlabel + "</header>";
                                            string str21 = !html2.Contains("$title") ? html2.Insert(startIndex5, str20) : html2.Replace("$title", newValue3);
                                            string newValue4 = "";
                                            if (dictionary2.ContainsKey(str17))
                                            {
                                                foreach (string str22 in dictionary2[str17])
                                                {
                                                    string str23 = Regex.Replace(str22, "\\s+", "-").Replace("/", "-");
                                                    num4 = str21.IndexOf("</body>");
                                                    string str24 = "<p><a href=\"/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str23.ToLower() + "-" + str18.ToLower() + "-" + shortlabel.ToLower() + ".html\" target=\"_blank\">" + str22 + "</a></p>" + Environment.NewLine;
                                                    newValue4 += str24;
                                                    using (StreamWriter streamWriter4 = System.IO.File.AppendText(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/" + str7 + "-" + str23.ToLower() + "-" + str18.ToLower() + "-" + shortlabel.ToLower() + ".html"))
                                                    {
                                                        string html3 = entity1.html;
                                                        num2 = html3.IndexOf("<head>") + 6;
                                                        string newValue5 = input + " " + str22 + " " + str17 + " " + shortlabel;
                                                        int startIndex6 = html3.IndexOf("<body>") + 6;
                                                        string str25 = "<header id='header' class='hoc clear'>" + input + " " + str22 + " " + str17 + " " + shortlabel + "</header>";
                                                        string str26 = (!html3.Contains("$title") ? html3.Insert(startIndex6, str25) : html3.Replace("$title", newValue5)).Replace("$state", shortlabel).Replace("$city", str17).Replace("$list", "").Replace("$neighborhood", str22);
                                                        string str27 = "<ul>";
                                                        if (((IEnumerable<string>)source2).Count<string>() > 15)
                                                        {
                                                            int[] numArray = new int[15];
                                                            for (int index8 = 0; index8 < 15; ++index8)
                                                            {
                                                                numArray[index8] = random.Next(0, ((IEnumerable<string>)source2).Count<string>() - 1);
                                                                for (int index9 = 0; index9 < index8; ++index9)
                                                                {
                                                                    if (numArray[index8] == numArray[index9])
                                                                    {
                                                                        --index8;
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            for (int index10 = 0; index10 < 15; ++index10)
                                                                str27 = str27 + "<li>" + source2[numArray[index10]] + "</li>";
                                                        }
                                                        else
                                                        {
                                                            foreach (string str28 in source2)
                                                                str27 = str27 + "<li>" + str28 + "</li>";
                                                        }
                                                        string newValue6 = str27 + "</ul>";
                                                        string str29 = str26.Replace("$keyword", newValue6).Replace("$content", stringList1[num3 % stringList1.Count]);
                                                        ++num3;
                                                        ((TextWriter)streamWriter4).WriteLine(str29);
                                                        ((TextWriter)streamWriter4).Close();
                                                    }
                                                }
                                            }
                                            string str30 = str21.Replace("$list", newValue4).Replace("$state", shortlabel).Replace("$city", str17).Replace("$neighborhood", "");
                                            string str31 = "<ul>";
                                            if (((IEnumerable<string>)source2).Count<string>() > 15)
                                            {
                                                int[] numArray = new int[15];
                                                for (int index11 = 0; index11 < 15; ++index11)
                                                {
                                                    numArray[index11] = random.Next(0, ((IEnumerable<string>)source2).Count<string>() - 1);
                                                    for (int index12 = 0; index12 < index11; ++index12)
                                                    {
                                                        if (numArray[index11] == numArray[index12])
                                                        {
                                                            --index11;
                                                            break;
                                                        }
                                                    }
                                                }
                                                for (int index13 = 0; index13 < 15; ++index13)
                                                    str31 = str31 + "<li>" + source2[numArray[index13]] + "</li>";
                                            }
                                            else
                                            {
                                                foreach (string str32 in source2)
                                                    str31 = str31 + "<li>" + str32 + "</li>";
                                            }
                                            string newValue7 = str31 + "</ul>";
                                            string str33 = str30.Replace("$keyword", newValue7).Replace("$content", stringList1[num3 % stringList1.Count]);
                                            ++num3;
                                            ((TextWriter)streamWriter3).WriteLine(str33);
                                            ((TextWriter)streamWriter3).Close();
                                        }
                                    }
                                }
                                string str34 = str16.Replace("$list", newValue2).Replace("$state", state).Replace("$city", "").Replace("$neighborhood", "");
                                string str35 = "<ul>";
                                if (((IEnumerable<string>)source2).Count<string>() > 15)
                                {
                                    int[] numArray = new int[15];
                                    for (int index14 = 0; index14 < 15; ++index14)
                                    {
                                        numArray[index14] = random.Next(0, ((IEnumerable<string>)source2).Count<string>() - 1);
                                        for (int index15 = 0; index15 < index14; ++index15)
                                        {
                                            if (numArray[index14] == numArray[index15])
                                            {
                                                --index14;
                                                break;
                                            }
                                        }
                                    }
                                    for (int index16 = 0; index16 < 15; ++index16)
                                        str35 = str35 + "<li>" + source2[numArray[index16]] + "</li>";
                                }
                                else
                                {
                                    foreach (string str36 in source2)
                                        str35 = str35 + "<li>" + str36 + "</li>";
                                }
                                string newValue8 = str35 + "</ul>";
                                string str37 = str34.Replace("$content", stringList1[num3 % stringList1.Count]).Replace("$keyword", newValue8);
                                ++num3;
                                ((TextWriter)streamWriter2).WriteLine(str37);
                                ((TextWriter)streamWriter2).Close();
                            }
                        }
                      ((TextWriter)streamWriter1).WriteLine(str11);
                        ((TextWriter)streamWriter1).Close();
                    }
                }
                using (StreamWriter streamWriter = System.IO.File.AppendText(Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/index.html"))
                {
                    ((TextWriter)streamWriter).WriteLine(str6);
                    ((TextWriter)streamWriter).Close();
                }
                string str38 = Directory.GetCurrentDirectory() + "/wwwroot/" + str2.Replace(" ", "-") + ".zip";
                string sourceDirectoryName = Directory.GetCurrentDirectory() + "/wwwroot/files/" + str2.Replace(" ", "-") + "/";
                if (System.IO.File.Exists(str38))
                    System.IO.File.Delete(str38);
                ZipFile.CreateFromDirectory(sourceDirectoryName, str38);
                htmltemplate entity2 = this._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<htmltemplate>();
                entity2.state = 0;
                this._context.htmltemplate.Update(entity2);
                this._context.SaveChanges();
            }
            catch (Exception ex)
            {
                htmltemplate entity = this._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == Convert.ToInt32(id))).SingleOrDefault<htmltemplate>();
                entity.state = 0;
                this._context.htmltemplate.Update(entity);
                this._context.SaveChanges();
            }
            return (IActionResult)this.RedirectToAction(nameof(HtmlBuilder), id);
        }

        [HttpPost]
        public async Task<JsonResult> CheckHtml(int id)
        {
            HomeController homeController = this;
            try
            {
                string data = homeController._context.htmltemplate.Where<htmltemplate>((Expression<Func<htmltemplate, bool>>)(i => i.id == id)).SingleOrDefault<htmltemplate>().state != 0 ? "{\"data\":\"loading\"}" : "{\"data\":\"success\"}";
                return homeController.Json((object)data);
            }
            catch
            {
                string data = "{\"data\":\"failed\"}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckTurl()
        {
            HomeController homeController = this;
            try
            {
                string data = homeController._context.twitterpost.SingleOrDefault<twitterpost>().state != 0 ? "{\"data\":\"loading\"}" : "{\"data\":\"success\"}";
                return homeController.Json((object)data);
            }
            catch
            {
                string data = "{\"data\":\"failed\"}";
                return homeController.Json((object)data);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Htmltemplate(
          string htmlcontent,
          string title,
          string DelTemp,
          string SaveTemp)
        {
            HomeController homeController = this;
            if (!string.IsNullOrEmpty(SaveTemp))
            {
                try
                {
                    if (title == "" || title == null)
                        return (IActionResult)homeController.RedirectToAction("template");
                    templates templates1 = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<templates>(homeController._context.templates.Where<templates>((Expression<Func<templates, bool>>)(i => i.name == title)), new CancellationToken());
                    if (templates1 != null)
                    {
                        templates templates2 = new templates();
                        templates1.name = title;
                        templates1.html = htmlcontent;
                        int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    }
                    else
                    {
                        homeController._context.templates.Add(new templates()
                        {
                            name = title,
                            html = htmlcontent
                        });
                        int num = await homeController._context.SaveChangesAsync(new CancellationToken());
                    }
                }
                catch (Exception ex)
                {
                }
            }
            if (!string.IsNullOrEmpty(DelTemp))
            {
                templates templates = await EntityFrameworkQueryableExtensions.SingleOrDefaultAsync<templates>(homeController._context.templates.Where<templates>((Expression<Func<templates, bool>>)(i => i.name == title)), new CancellationToken());
                homeController._context.templates.Remove(homeController._context.templates.Find((object)templates.id));
                int num = await homeController._context.SaveChangesAsync(new CancellationToken());
            }
            return (IActionResult)homeController.RedirectToAction("template");
        }

        public async Task<IActionResult> TweetUrl(string id)
        {
            HomeController homeController = this;
            List<tweeturl> result = new List<tweeturl>();
            foreach (tweeturl tweeturl in (IEnumerable<tweeturl>)await EntityFrameworkQueryableExtensions.ToListAsync<tweeturl>((IQueryable<tweeturl>)homeController._context.tweeturl, new CancellationToken()))
                result.Add(tweeturl);
            IActionResult actionResult = (IActionResult)homeController.View((object)result);
            result = (List<tweeturl>)null;
            return actionResult;
        }

        [Route("Time")]
        public string Time() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public int CompareStrings(string s1, string s2)
        {
            int num1 = int.Parse(s1.Split("\t", StringSplitOptions.None)[0]);
            int num2 = int.Parse(s2.Split("\t", StringSplitOptions.None)[0]);
            if (num2 > num1)
                return 1;
            return num2 < num1 ? -1 : 0;
        }

        private bool trafficjobExists(int id) => this._context.trafficjob.Any<trafficjob>((Expression<Func<trafficjob, bool>>)(e => e.id == id));
    }
}