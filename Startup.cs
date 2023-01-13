using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Traffic.Data;
using Traffic.Models;
using Traffic.Services;

namespace Traffic
{
    public class Startup
    {
        private string connectString = "DefaultConnection2";
        public static DateTime LastRefreshedRunningTime;
        public static string client_id = "949187247496-sso7ev4mb21v8b3pjg0gvkg793akpesk.apps.googleusercontent.com";
        public static string client_secret = "J0AF1QyOPIq--sOYqMhen8bs";

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.connectString = "DefaultConnection2";
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new BackgroundWorker.DoWorkEventHandler(TrafficMainprocAsync);
            backgroundWorker.RunWorker((object[])null);
        }

        public static int[] GetHourLoad(List<dailystate> todaylist)
        {
            int[] hourLoad = new int[24];
            Array.Clear((Array)hourLoad, 0, hourLoad.Length);
            foreach (dailystate dailystate in todaylist)
            {
                DateTime predictTime = dailystate.predict_time;
                if (0 <= predictTime.Hour)
                {
                    predictTime = dailystate.predict_time;
                    if (predictTime.Hour < 24)
                    {
                        int[] numArray = hourLoad;
                        predictTime = dailystate.predict_time;
                        int hour = predictTime.Hour;
                        ++numArray[hour];
                    }
                }
            }
            return hourLoad;
        }

        public static void AddScheduleForToday(
          ApplicationDbContext context,
          int jobid,
          bool runnow = false,
          bool impressions = false)
        {
            Random random = new Random();
            trafficjob entity = (trafficjob)null;
            try
            {
                entity = context.trafficjob.SingleOrDefault<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == jobid));
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception F {0}", (object)ex));
            }
            if (entity == null)
                return;
            context.Entry<trafficjob>(entity).Reload();
            if (runnow)
            {
                context.Add<dailystate>(new dailystate()
                {
                    job_id = entity.id,
                    state = 0,
                    isrunning = false,
                    predict_time = DateTime.Now.Date,
                    consfailcount = 0
                });
            }
            else
            {
                int[] hourLoad = Startup.GetHourLoad(context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date)).ToList<dailystate>());
                for (int index1 = 0; index1 < entity.session_count; ++index1)
                {
                    int result1 = 0;
                    int.TryParse(entity.start_time, out result1);
                    int result2 = 0;
                    int.TryParse(entity.end_time, out result2);
                    string id = entity.time_zone;
                    if (result2 > result1)
                    {
                        if (DateTime.Now.Hour <= result2)
                        {
                            if (DateTime.Now.Hour > result1)
                                result1 = DateTime.Now.Hour;
                        }
                        else
                            break;
                    }
                    else
                    {
                        result1 = DateTime.Now.Hour;
                        result2 = 24;
                    }
                    int index2 = result1;
                    for (int index3 = result1 + 1; index3 < result2; ++index3)
                    {
                        if (hourLoad[index2] > hourLoad[index3])
                            index2 = index3;
                    }
                    TimeSpan timeSpan = TimeSpan.FromSeconds((double)random.Next(index2 * 60 * 60, (index2 + 1) * 60 * 60));
                    DateTime dateTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    if (string.IsNullOrEmpty(id))
                        id = "GMT Standard Time";
                    TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(id);
                    DateTime dateTime2 = TimeZoneInfo.ConvertTime(dateTime1, TimeZoneInfo.Local, systemTimeZoneById);
                    context.Add<dailystate>(new dailystate()
                    {
                        job_id = entity.id,
                        state = 0,
                        isrunning = false,
                        predict_time = dateTime2,
                        consfailcount = 0
                    });
                    ++hourLoad[index2];
                }
            }
            context.SaveChanges();
        }

        public static void RemoveScheduleForToday(ApplicationDbContext context, int jobid)
        {
            dailystate[] array = context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date && m.job_id == jobid && (m.state == 0 || m.state == 1 && m.consfailcount < 3))).ToArray<dailystate>();
            context.dailystate.RemoveRange(array);
        }

        public static void EditScheduleForToday(ApplicationDbContext context, int jobid)
        {
            Random random = new Random();
            trafficjob entity1 = (trafficjob)null;
            try
            {
                entity1 = context.trafficjob.SingleOrDefault<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == jobid));
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception F {0}", (object)ex));
            }
            if (entity1 == null || entity1.group_id == -1 || !entity1.switch_on)
                return;
            context.Entry<trafficjob>(entity1).Reload();
            List<dailystate> list1 = context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date && m.job_id == jobid)).ToList<dailystate>();
            int num = 0;
            foreach (dailystate dailystate in list1)
            {
                if (dailystate.state != 0)
                    ++num;
            }
            if (num >= entity1.session_count)
            {
                foreach (dailystate entity2 in list1)
                {
                    if (entity2.state == 0 || entity2.state == 1 && entity2.consfailcount < 3 || entity2.state == 3)
                        context.Remove<dailystate>(entity2);
                }
            }
            else
            {
                int[] hourLoad = Startup.GetHourLoad(context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date)).ToList<dailystate>());
                List<dailystate> list2;
                for (list2 = list1.Where<dailystate>((Func<dailystate, bool>)(state => state.state == 0 || state.state == 1 && state.consfailcount < 3 || state.state == 3)).ToList<dailystate>(); num < entity1.session_count && list2.Count<dailystate>() > 0; ++num)
                {
                    dailystate dailystate = list2[0];
                    list2.RemoveAt(0);
                }
                if (list2.Count<dailystate>() > 0)
                {
                    foreach (dailystate entity3 in list2)
                        context.Remove<dailystate>(entity3);
                }
                for (int index1 = num; index1 < entity1.session_count; ++index1)
                {
                    int result1 = 0;
                    int.TryParse(entity1.start_time, out result1);
                    int result2 = 0;
                    int.TryParse(entity1.end_time, out result2);
                    string id = entity1.time_zone;
                    if (result2 > result1)
                    {
                        if (DateTime.Now.Hour <= result2)
                        {
                            if (DateTime.Now.Hour > result1)
                                result1 = DateTime.Now.Hour;
                        }
                        else
                            break;
                    }
                    else
                    {
                        result1 = DateTime.Now.Hour;
                        result2 = 24;
                    }
                    int index2 = result1;
                    for (int index3 = result1 + 1; index3 < result2; ++index3)
                    {
                        if (hourLoad[index2] > hourLoad[index3])
                            index2 = index3;
                    }
                    TimeSpan timeSpan = TimeSpan.FromSeconds((double)random.Next(index2 * 60 * 60, (index2 + 1) * 60 * 60));
                    DateTime dateTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    if (string.IsNullOrEmpty(id))
                        id = "GMT Standard Time";
                    TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(id);
                    DateTime dateTime2 = TimeZoneInfo.ConvertTime(dateTime1, TimeZoneInfo.Local, systemTimeZoneById);
                    dailystate entity4 = new dailystate();
                    entity4.job_id = entity1.id;
                    entity4.state = 0;
                    entity4.isrunning = false;
                    entity4.predict_time = dateTime2;
                    entity4.consfailcount = 0;
                    context.Add<dailystate>(entity4);
                    context.dailystate.Add(entity4);
                    ++hourLoad[index2];
                }
            }
            context.SaveChanges();
        }

        public static void RefreshRunningForToday(ApplicationDbContext context)
        {
            Random random = new Random();
            if (!(Startup.LastRefreshedRunningTime.Date < DateTime.Now.Date))
                return;
            List<ApplicationUser> list1 = context.Users.ToList<ApplicationUser>();
            List<dailystate> list2 = context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.AddDays(-1.0).Date && m.isrunning == false && m.state == 0)).ToList<dailystate>();
            dailystate[] array = context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date)).ToArray<dailystate>();
            context.dailystate.RemoveRange(array);
            try
            {
                foreach (dailystate dailystate in list2)
                {
                    dailystate state = dailystate;
                    trafficjob trafficjob;
                    try
                    {
                        trafficjob = context.trafficjob.Single<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == state.job_id));
                    }
                    catch
                    {
                        continue;
                    }
                    if (trafficjob.group_id != -1 && trafficjob.switch_on)
                    {
                        bool flag = true;
                        foreach (IdentityUser<string> identityUser in list1)
                        {
                            if (identityUser.Id == trafficjob.user_id)
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (!flag)
                            context.dailystate.Add(new dailystate()
                            {
                                job_id = state.job_id,
                                state = 0,
                                predict_time = DateTime.Now.Date,
                                isoldjourney = true
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception B {0}", (object)ex));
            }
            List<trafficjob> list3 = context.trafficjob.ToList<trafficjob>();
            int[] numArray = new int[24];
            for (int index = 0; index < 24; ++index)
                numArray[index] = 0;
            try
            {
                List<trafficjob> trafficjobList = new List<trafficjob>();
                foreach (trafficjob entity1 in list3)
                {
                    context.Entry<trafficjob>(entity1).Reload();
                    if (entity1 != null && entity1.group_id != -1 && entity1.switch_on)
                    {
                        bool flag = true;
                        foreach (IdentityUser<string> identityUser in list1)
                        {
                            if (identityUser.Id == entity1.user_id)
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            int result1 = 0;
                            int.TryParse(entity1.start_time, out result1);
                            int result2 = 0;
                            int.TryParse(entity1.end_time, out result2);
                            string id = entity1.time_zone;
                            if (result2 > result1)
                            {
                                for (int index1 = 0; index1 < entity1.session_count; ++index1)
                                {
                                    int index2 = result1;
                                    for (int index3 = result1 + 1; index3 < result2; ++index3)
                                    {
                                        if (numArray[index2] > numArray[index3])
                                            index2 = index3;
                                    }
                                    TimeSpan timeSpan = TimeSpan.FromSeconds((double)random.Next(index2 * 60 * 60, (index2 + 1) * 60 * 60));
                                    DateTime dateTime1 = DateTime.Now;
                                    ref DateTime local = ref dateTime1;
                                    DateTime now = DateTime.Now;
                                    int year = now.Year;
                                    now = DateTime.Now;
                                    int month = now.Month;
                                    now = DateTime.Now;
                                    int day = now.Day;
                                    int hours = timeSpan.Hours;
                                    int minutes = timeSpan.Minutes;
                                    int seconds = timeSpan.Seconds;
                                    local = new DateTime(year, month, day, hours, minutes, seconds);
                                    if (string.IsNullOrEmpty(id))
                                        id = "GMT Standard Time";
                                    TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(id);
                                    DateTime dateTime2 = TimeZoneInfo.ConvertTime(dateTime1, TimeZoneInfo.Local, systemTimeZoneById);
                                    dailystate entity2 = new dailystate();
                                    entity2.job_id = entity1.id;
                                    entity2.state = 0;
                                    entity2.isrunning = false;
                                    entity2.predict_time = dateTime2;
                                    entity2.consfailcount = 0;
                                    context.Add<dailystate>(entity2);
                                    context.dailystate.Add(entity2);
                                    ++numArray[index2];
                                }
                            }
                            else
                                trafficjobList.Add(entity1);
                        }
                    }
                }
                foreach (trafficjob trafficjob in trafficjobList)
                {
                    for (int index4 = 0; index4 < trafficjob.session_count; ++index4)
                    {
                        int index5 = 0;
                        for (int index6 = 1; index6 < 24; ++index6)
                        {
                            if (numArray[index5] > numArray[index6])
                                index5 = index6;
                        }
                        TimeSpan timeSpan = TimeSpan.FromSeconds((double)random.Next(index5 * 60 * 60, (index5 + 1) * 60 * 60));
                        DateTime dateTime = DateTime.Now;
                        ref DateTime local = ref dateTime;
                        DateTime now = DateTime.Now;
                        int year = now.Year;
                        now = DateTime.Now;
                        int month = now.Month;
                        now = DateTime.Now;
                        int day = now.Day;
                        int hours = timeSpan.Hours;
                        int minutes = timeSpan.Minutes;
                        int seconds = timeSpan.Seconds;
                        local = new DateTime(year, month, day, hours, minutes, seconds);
                        context.dailystate.Add(new dailystate()
                        {
                            job_id = trafficjob.id,
                            state = 0,
                            isrunning = false,
                            predict_time = dateTime,
                            consfailcount = 0
                        });
                        ++numArray[index5];
                    }
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception E {0}", (object)ex));
            }
            Startup.LastRefreshedRunningTime = DateTime.Now;
        }

        public static void UpdateJourneyResult(ApplicationDbContext context, log result)
        {
            try
            {
                if (!string.IsNullOrEmpty(result.agent) && result.agent.Length > 500)
                    result.agent = result.agent.Substring(0, 499);
                if (!string.IsNullOrEmpty(result.proxy) && result.proxy.Length > 500)
                    result.proxy = result.proxy.Substring(0, 499);
                context.Add<log>(result);
                trafficjob job = context.trafficjob.SingleOrDefault<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == result.job_id));
                if (job == null)
                {
                    context.SaveChanges();
                }
                else
                {
                    DbSet<dailystate> dailystate1 = context.dailystate;
                    Expression<Func<dailystate, bool>> predicate1 = (Expression<Func<dailystate, bool>>)(m => m.job_id == job.id && m.isrunning == true);
                    foreach (dailystate dailystate2 in dailystate1.Where<dailystate>(predicate1).ToList<dailystate>())
                    {
                        dailystate jobRunningState = dailystate2;
                        context.Entry<dailystate>(jobRunningState).Reload();
                        if (!string.IsNullOrEmpty(result.worker))
                        {
                            if (jobRunningState.runner_id != result.worker)
                                continue;
                        }
                        else if (!string.IsNullOrEmpty(jobRunningState.runner_id))
                            continue;
                        if (result.result)
                        {
                            jobRunningState.consfailcount = 0;
                            jobRunningState.state = 2;
                        }
                        else
                        {
                            ++jobRunningState.consfailcount;
                            jobRunningState.state = 1;
                        }
                        jobRunningState.end_time = DateTime.Now;
                        jobRunningState.isrunning = false;
                        context.Update<dailystate>(jobRunningState);
                        if (jobRunningState.state == 1 && jobRunningState.consfailcount >= 3)
                        {
                            DbSet<dailystate> dailystate3 = context.dailystate;
                            Expression<Func<dailystate, bool>> predicate2 = (Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date && m.job_id == jobRunningState.job_id && m.state == 0 && m.isrunning != true);
                            foreach (dailystate entity in dailystate3.Where<dailystate>(predicate2).ToList<dailystate>())
                            {
                                entity.state = 3;
                                context.Update<dailystate>(entity);
                            }
                        }
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception A {0}", (object)ex));
            }
        }

        public static void ResetRunningFlagJourneiesForUser(ApplicationDbContext context, string userId)
        {
            try
            {
                DbSet<dailystate> dailystate = context.dailystate;
                Expression<Func<dailystate, bool>> predicate = (Expression<Func<dailystate, bool>>)(m => m.runner_id == userId && m.isrunning);
                foreach (dailystate entity in dailystate.Where<dailystate>(predicate).ToList<dailystate>())
                {
                    entity.isrunning = false;
                    context.Update<dailystate>(entity);
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception C {0}", (object)ex));
            }
        }

        public static async Task<string> GetRefreshedToken(string refresh_token)
        {
            string str = nameof(refresh_token);
            return await (await new HttpClient().PostAsync("https://www.googleapis.com/oauth2/v4/token", (HttpContent)new StringContent(string.Format("{{\"client_id\":\"{0}\",\"client_secret\":\"{1}\",\"refresh_token\":\"{2}\",\"grant_type\":\"{3}\"}}", (object)Startup.client_id, (object)Startup.client_secret, (object)refresh_token, (object)str), Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
        }

        public static async Task<bool> DoGmbPostinQuery(ApplicationDbContext context)
        {
            List<gmbpost_query> list = context.gmbpost_query.Where<gmbpost_query>((Expression<Func<gmbpost_query, bool>>)(m => m.time <= DateTime.Now)).ToList<gmbpost_query>();
            HttpClient client = new HttpClient();
            string refresh_token = "";
            string authorization = "";
            foreach (gmbpost_query query in list)
            {
                if (string.IsNullOrEmpty(authorization) || refresh_token != query.refreshtoken)
                {
                    try
                    {
                        JObject jobject = JObject.Parse(await Startup.GetRefreshedToken(query.refreshtoken));
                        string str = jobject["access_token"].ToString();
                        jobject["token_type"].ToString();
                        refresh_token = query.refreshtoken;
                        authorization = str;
                    }
                    catch
                    {
                    }
                }
                if (!string.IsNullOrEmpty(authorization))
                {
                    try
                    {
                        string str1 = string.Format("{{\"languageCode\": \"{0}\",\"summary\": \"{1}\",\"callToAction\": {{\"actionType\": \"{2}\",\"url\": \"{3}\",}},\"media\": [{{\"mediaFormat\": \"{4}\",\"sourceUrl\": \"{5}\",}}],}}", (object)query.languagecode, (object)query.summary, (object)query.actiontype, (object)query.actionurl, (object)query.mediaformat, (object)query.mediaurl);
                        string str2 = await (await client.PostAsync(string.Format("https://mybusiness.googleapis.com/v4/{0}/localPosts?access_token={1}", (object)query.locationname, (object)authorization), (HttpContent)new StringContent(str1, Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
                    }
                    catch
                    {
                    }
                }
                context.Remove<gmbpost_query>(query);
                int num = await context.SaveChangesAsync(new CancellationToken());
            }
            bool flag = true;
            client = (HttpClient)null;
            refresh_token = (string)null;
            authorization = (string)null;
            return flag;
        }

        public static trafficjob PickNextJourney(ApplicationDbContext context, string userId = "")
        {
            trafficjob entity = (trafficjob)null;
            try
            {
                Startup.RefreshRunningForToday(context);
                IQueryable<dailystate> source = context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.predict_time.Date == DateTime.Now.Date));
                Expression<Func<dailystate, DateTime>> keySelector = (Expression<Func<dailystate, DateTime>>)(o => o.predict_time);
                foreach (dailystate dailystate in source.OrderBy<dailystate, DateTime>(keySelector).ToList<dailystate>())
                {
                    dailystate state = dailystate;
                    context.Entry<dailystate>(state).Reload();
                    if (!state.isrunning && state.state != 2 && (state.state != 1 || state.consfailcount < 3) && state.state != 3)
                    {
                        state.isrunning = true;
                        state.start_time = DateTime.Now;
                        state.runner_id = userId;
                        context.Update<dailystate>(state);
                        try
                        {
                            entity = context.trafficjob.SingleOrDefault<trafficjob>((Expression<Func<trafficjob, bool>>)(m => m.id == state.job_id));
                            context.Entry<trafficjob>(entity).Reload();
                        }
                        catch
                        {
                        }
                        context.SaveChanges();
                        break;
                    }
                }
                return entity;
            }
            catch (Exception ex)
            {
                SettingManager.Logger(string.Format("Exception D {0}", (object)ex));
            }
            return (trafficjob)null;
        }

        public static string selectproxy(trafficjob job)
        {
            Random rnd = new Random();
            string proxySetting = job.proxy_setting;
            string str1 = ProxyRotator.NewProxy();
            if (!string.IsNullOrEmpty(proxySetting))
            {
                if (proxySetting.Contains(':') || !proxySetting.Contains(','))
                {
                    foreach (string str2 in ((IEnumerable<string>)proxySetting.Split("\n", StringSplitOptions.None)).OrderBy<string, int>((Func<string, int>)(x => rnd.Next())).ToArray<string>())
                    {
                        if (!string.IsNullOrEmpty(str2))
                        {
                            Regex regex1 = new Regex("^\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}:\\d{1,5}$");
                            Regex regex2 = new Regex("^[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\\'\\/\\\\\\+&amp;%\\$#_]*)?$");
                            if (regex1.IsMatch(str2.Trim()))
                            {
                                str1 = str2.Trim();
                                break;
                            }
                            if (regex2.IsMatch(str2.Trim()))
                            {
                                str1 = str2.Trim();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (string str3 in ((IEnumerable<string>)proxySetting.Split("\n", StringSplitOptions.None)).OrderBy<string, int>((Func<string, int>)(x => rnd.Next())).ToArray<string>())
                    {
                        if (!(str3 == ""))
                        {
                            string str4 = str3.Split(',', StringSplitOptions.None)[0];
                            ProcessStartInfo processStartInfo = new ProcessStartInfo();
                            Process process = new Process();
                            processStartInfo.FileName = "cmd";
                            processStartInfo.CreateNoWindow = false;
                            processStartInfo.UseShellExecute = false;
                            processStartInfo.RedirectStandardInput = true;
                            processStartInfo.RedirectStandardOutput = true;
                            processStartInfo.RedirectStandardError = true;
                            process.StartInfo = processStartInfo;
                            process.Start();
                            ((TextWriter)process.StandardInput).Write("curl -U user-tianresiden-country-us-city-" + str4.ToLower() + "-session-randomstring123:tiantian -x gate.smartproxy.com:7000 https://ipinfo.io" + Environment.NewLine);
                            ((TextWriter)process.StandardInput).Close();
                            string end = ((TextReader)process.StandardOutput).ReadToEnd();
                            process.WaitForExit();
                            process.Close();
                            string str5 = "";
                            if (Regex.Match(end, "ip\": \"(?<VAL>[^\"]*)").Groups["VAL"].Value != "")
                                str5 = Regex.Match(end, "ip\": \"(?<VAL>[^\"]*)").Groups["VAL"].Value + ":7000";
                            str1 = str5.Trim();
                            break;
                        }
                    }
                }
            }
            return str1;
        }

        public static string GetJobPath(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/project/" + JobName;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private async void TrafficMainprocAsync(params object[] arguments)
        {
            Random random = new Random();
            Startup.LastRefreshedRunningTime = DateTime.Now;
            DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql<ApplicationDbContext>(this.Configuration.GetConnectionString(this.connectString));
            ApplicationDbContext _context = new ApplicationDbContext(optionsBuilder.Options);
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
            string currentDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(currentDirectory + "/project"))
                Directory.CreateDirectory(currentDirectory + "/project");
            string path = Directory.GetCurrentDirectory() + "/FakeName.csv";
            if (File.Exists(path))
            {
                BrowserAgent.FakeInfo = File.ReadAllLines(path);
                BrowserAgent.FakeSchema = new Hashtable();
                string[] strArray = BrowserAgent.FakeInfo[0].Split(",", StringSplitOptions.None);
                for (int index = 0; index < strArray.Length; ++index)
                    BrowserAgent.FakeSchema[(object)strArray[index]] = (object)index;
            }
            List<dailystate> list1 = _context.dailystate.Where<dailystate>((Expression<Func<dailystate, bool>>)(m => m.isrunning == true)).ToList<dailystate>();
            try
            {
                foreach (dailystate entity in list1)
                {
                    entity.isrunning = false;
                    _context.dailystate.Update(entity);
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            while (true)
            {
                int num = await Startup.DoGmbPostinQuery(_context) ? 1 : 0;
                trafficjob job = Startup.PickNextJourney(_context);
                if (job != null)
                {
                    try
                    {
                        DisplayAttribute attr = CustomAttributeExtensions.GetCustomAttribute<DisplayAttribute>((MemberInfo)job.search_engine.GetType().GetField(job.search_engine.ToString()));
                        if (job.journey_option.StartsWith("Knowledge") || job.journey_option.StartsWith("GMB"))
                        {
                            if (job.agent_kind == trafficjob.AGENT_KIND.BOTH)
                                job.agent_kind = trafficjob.AGENT_KIND.DESKTOP;
                            else if (job.agent_kind == trafficjob.AGENT_KIND.MOBILE)
                                job.agent_age = trafficjob.AGENT_AGE.NEW;
                        }
                        string agent_desc = string.Empty;
                        string agent = BrowserAgent.SpinAgent((int)job.agent_kind, (int)job.agent_age, out agent_desc);
                        string _proxy = Startup.selectproxy(job);
                        string str1 = "";
                        if (!string.IsNullOrEmpty(job.location) && job.journey_option == "Normal + Location")
                        {
                            string[] strArray1 = job.location.Split("\r\n", StringSplitOptions.None);
                            if (strArray1[0].Split(",", StringSplitOptions.None).Length == 4)
                            {
                                foreach (string str2 in strArray1)
                                {
                                    string[] strArray2 = str2.Split(",", StringSplitOptions.None);
                                    if (strArray2[0] != "")
                                        str1 = str1 + strArray2[0] + " " + strArray2[1] + ",";
                                }
                                if (str1 != "")
                                    str1 = str1.Substring(0, str1.Length - 1);
                                object[] objArray = new object[7]
                                {
                  (object) job,
                  (object) ("https://" + attr.Name),
                  (object) agent,
                  (object) _proxy,
                  (object) agent_desc,
                  null,
                  (object) str1
                                };
                                Startup.UpdateJourneyResult(_context, await BrowserAgent.worker_DoWork(objArray));
                            }
                            else
                            {
                                string[] strArray = strArray1;
                                for (int index = 0; index < strArray.Length; ++index)
                                {
                                    string[] strs = strArray[index].Split(",", StringSplitOptions.None);
                                    location llt = _context.location.Where<location>((Expression<Func<location, bool>>)(i => i.depth == 2 && i.label == strs[1].Replace(" ", ""))).SingleOrDefault<location>();
                                    int loc_id = llt.id;
                                    llt = _context.location.Where<location>((Expression<Func<location, bool>>)(i => i.depth == 3 && i.parentid == loc_id && i.label == strs[0])).SingleOrDefault<location>();
                                    List<location> list2 = (await EntityFrameworkQueryableExtensions.ToListAsync<location>(_context.location.Where<location>((Expression<Func<location, bool>>)(i => i.depth == 4 && i.parentid == llt.id)), new CancellationToken())).ToList<location>();
                                    list2.Insert(0, llt);
                                    object[] objArray = new object[7]
                                    {
                    (object) job,
                    (object) ("https://" + attr.Name),
                    (object) agent,
                    (object) _proxy,
                    (object) agent_desc,
                    (object) list2,
                    (object) ""
                                    };
                                    Startup.UpdateJourneyResult(_context, await BrowserAgent.worker_DoWork(objArray));
                                }
                                strArray = (string[])null;
                            }
                        }
                        else
                        {
                            if (str1 != "")
                                str1.Substring(0, str1.Length - 1);
                            object[] objArray = new object[7]
                            {
                (object) job,
                (object) ("https://" + attr.Name),
                (object) agent,
                (object) _proxy,
                (object) agent_desc,
                null,
                (object) ""
                            };
                            Startup.UpdateJourneyResult(_context, await BrowserAgent.worker_DoWork(objArray));
                        }
                        attr = (DisplayAttribute)null;
                        agent_desc = (string)null;
                        agent = (string)null;
                        _proxy = (string)null;
                    }
                    catch (Exception ex)
                    {
                    }
                }
                Thread.Sleep(3000);
                job = (trafficjob)null;
            }
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>((Action<DbContextOptionsBuilder>)(options => options.UseMySql(this.Configuration.GetConnectionString(this.connectString))));
            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddMvc();
        }

        public async void Configure(
          IApplicationBuilder app,
          IHostingEnvironment env,
          ILoggerFactory loggerFactory,
          IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
                app.UseExceptionHandler("/Home/Error");
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc((Action<IRouteBuilder>)(routes => routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}")));
            this.CreateRoles(serviceProvider).Wait();
        }

        private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            RoleManager<IdentityRole> RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<ApplicationUser> UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            string[] strArray = new string[3]
            {
        "Admin",
        "Manager",
        "Member"
            };
            for (int index = 0; index < strArray.Length; ++index)
            {
                string roleName = strArray[index];
                if (!await RoleManager.RoleExistsAsync(roleName))
                {
                    IdentityResult async = await RoleManager.CreateAsync(new IdentityRole(roleName));
                }
                roleName = (string)null;
            }
            strArray = (string[])null;
            ApplicationUser applicationUser = new ApplicationUser();
            applicationUser.UserName = this.Configuration.GetSection("UserSettings")["UserEmail"];
            applicationUser.Email = this.Configuration.GetSection("UserSettings")["UserEmail"];
            ApplicationUser poweruser = applicationUser;
            string UserPassword = this.Configuration.GetSection("UserSettings")["UserPassword"];
            if (await UserManager.FindByEmailAsync(this.Configuration.GetSection("UserSettings")["UserEmail"]) != null)
            {
                RoleManager = (RoleManager<IdentityRole>)null;
                UserManager = (UserManager<ApplicationUser>)null;
                poweruser = (ApplicationUser)null;
                UserPassword = (string)null;
            }
            else if (!(await UserManager.CreateAsync(poweruser, UserPassword)).Succeeded)
            {
                RoleManager = (RoleManager<IdentityRole>)null;
                UserManager = (UserManager<ApplicationUser>)null;
                poweruser = (ApplicationUser)null;
                UserPassword = (string)null;
            }
            else
            {
                IdentityResult roleAsync = await UserManager.AddToRoleAsync(poweruser, "Admin");
                RoleManager = (RoleManager<IdentityRole>)null;
                UserManager = (UserManager<ApplicationUser>)null;
                poweruser = (ApplicationUser)null;
                UserPassword = (string)null;
            }
        }
    }
}