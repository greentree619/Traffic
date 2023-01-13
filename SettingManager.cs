using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Traffic.Models;

namespace Traffic
{
    public class SettingManager
    {
        public static string projectDirectory = "project";
        public static string settingName = "settings.config";
        public static string proxysettingName = "proxysetting.config";
        public static string[] JoureyOptions = new string[36]
        {
      "Direct (Google)",
      "Direct (Google) with Google Form",
      "Direct (Any)",
      "Direct Product",
      "G - Site",
      "Super G-Site",
      "Normal",
      "Normal with Google Form",
      "Normal Brand",
      "Normal Brand with Google Form",
      "Normal + Location",
      "Optimus",
      "Optimus with Google Form",
      "Company",
      "Company with Google Form",
      "Knowledge",
      "Knowledge with Google Form",
      "GMB",
      "GMB with Google Form",
      "Brand",
      "Brand with Google Form",
      "T1",
      "T1 with Google Form",
      "T1 Google News",
      "T1 Google News with Google Form",
      "T1 Company",
      "T2 to T1",
      "T2 to T1 with Google Form",
      "ImageSearch",
      "ImageSearch with Google Form",
      "VideoSearch",
      "YoutubeSearch",
      "CustomSearch",
      "CustomSearch with Google Form",
      "Spike",
      "Mymap with that function"
        };

        public static void Logger(string Log)
        {
            try
            {
                using (StreamWriter streamWriter = File.AppendText(Directory.GetCurrentDirectory() + "/logfile.txt"))
                    ((TextWriter)streamWriter).WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]  " + Log);
            }
            catch
            {
            }
        }

        public static bool WithGoogleForm(string Journey) => Journey.Contains("with Google Form");

        public static bool IsRunning(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + SettingManager.settingName;
            if (!File.Exists(path))
                return false;
            return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None).AppSettings.Settings["start"].Value.CompareTo("1") == 0;
        }

        public static DateTime SetNextTime(trafficjob job, int successcount)
        {
            DateTime.Now.ToString("yy-MM-dd");
            string id = job.time_zone;
            string startTime = job.start_time;
            string endTime = job.end_time;
            int result1 = 0;
            int.TryParse(startTime, out result1);
            int result2 = 0;
            int.TryParse(endTime, out result2);
            int num1 = 86400;
            if (result2 > result1)
                num1 = (result2 - result1) * 60 * 60;
            int num2 = num1 / job.session_count;
            int num3 = new Random().Next(num2 * successcount, num2 * (successcount + 1));
            if (result2 > result1)
                num3 += result1 * 60 * 60;
            TimeSpan timeSpan = TimeSpan.FromSeconds((double)num3);
            timeSpan.ToString("hh\\:mm\\:ss");
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
            if (string.IsNullOrEmpty(id))
                id = "GMT Standard Time";
            TimeZoneInfo systemTimeZoneById = TimeZoneInfo.FindSystemTimeZoneById(id);
            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, systemTimeZoneById);
        }

        public static string SetNowTime(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + SettingManager.settingName;
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            configuration.AppSettings.Settings["next"].Value = str;
            configuration.Save(ConfigurationSaveMode.Modified);
            return str;
        }

        public static string GetNextTime(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + SettingManager.settingName;
            if (!File.Exists(path))
                return "";
            return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None).AppSettings.Settings["next"].Value;
        }

        public static string GetProxySetting(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + SettingManager.proxysettingName;
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string proxySetting = "";
            try
            {
                proxySetting = configuration.AppSettings.Settings["proxysetting"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("proxysetting", "");
            }
            return proxySetting;
        }

        public static string GetJourneyOption(string JobName)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + SettingManager.settingName;
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string journeyOption = "";
            try
            {
                journeyOption = configuration.AppSettings.Settings["JourneyOption"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("JourneyOption", "");
            }
            return journeyOption;
        }

        public static string GetFormFields(string JobName, string JourneyOption)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + JourneyOption + ".config";
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string formFields = "";
            try
            {
                formFields = configuration.AppSettings.Settings["form_fields"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("form_fields", "");
            }
            return formFields;
        }

        public static string GetTimeZone(string JobName, string JourneyOption)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + JourneyOption + ".config";
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string timeZone = "";
            try
            {
                timeZone = configuration.AppSettings.Settings["TimeZone"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("TimeZone", "");
            }
            return timeZone;
        }

        public static string GetStartTime(string JobName, string JourneyOption)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + JourneyOption + ".config";
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string startTime = "";
            try
            {
                startTime = configuration.AppSettings.Settings["StartTime"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("StartTime", "");
            }
            return startTime;
        }

        public static string GetEndTime(string JobName, string JourneyOption)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + JourneyOption + ".config";
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string endTime = "";
            try
            {
                endTime = configuration.AppSettings.Settings["EndTime"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("EndTime", "");
            }
            return endTime;
        }

        public static string GetJourneyURL(string JobName, string JourneyOption)
        {
            string path = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName + "/" + JourneyOption + ".config";
            if (!File.Exists(path))
                return "";
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = path
            }, ConfigurationUserLevel.None);
            string journeyUrl = "";
            try
            {
                journeyUrl = configuration.AppSettings.Settings["JourneyURL"].Value;
            }
            catch
            {
                configuration.AppSettings.Settings.Add("JourneyURL", "");
            }
            return journeyUrl;
        }

        public static string GetAllJourney(string JobName)
        {
            string str1 = Directory.GetCurrentDirectory() + "/" + SettingManager.projectDirectory + "/" + JobName;
            string str2 = "";
            foreach (string joureyOption in SettingManager.JoureyOptions)
            {
                if (str2.Length > 0)
                    str2 += ",";
                string str3 = "";
                string str4 = "";
                string path = str1 + "/" + joureyOption + ".config";
                if (File.Exists(path))
                {
                    System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
                    {
                        ExeConfigFilename = path
                    }, ConfigurationUserLevel.None);
                    try
                    {
                        str3 = configuration.AppSettings.Settings["JourneyURL"].Value;
                        str4 = configuration.AppSettings.Settings["form_fields"].Value;
                    }
                    catch
                    {
                    }
                }
                str2 = str2 + "'" + joureyOption + "': { 'JourneyURL': '" + str3 + "', 'form_fields': '" + str4 + "' }";
            }
            return "{" + str2 + "}";
        }

        public static List<string> LineSplitter(string line) => ((IEnumerable<string>)new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))").Split(line)).ToList<string>();
    }
}