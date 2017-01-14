using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.WindowsAzure.ServiceRuntime;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using WSHelpers;
using Microsoft.WindowsAzure.StorageClient;

namespace Coolftc.WTClimate
{
    public class Instrumentation
    {
        private MemcachedClient cacheClient;
        private DateTime resetWuApi;
        private DateTime oneWeekPast;
        private enum COUNT_RANGE { TODAY, YESTERDAY, WEEK, AVG_WEEK, UNKNOWN };

        // Handy configuration values.
        public double MAX_A_DAY;
        public double TILL_NEW;             // in seconds 
        public double TILL_STALE;
        public double TILL_DEATH;
        public string CACHE_ALIAS;

        public Instrumentation()
        {
            // Calculate start of Weather Underground day and other times.
            TimeZoneInfo NYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            int eastCoastMidnight = Math.Abs(NYTimeZone.BaseUtcOffset.Hours);
            resetWuApi = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, eastCoastMidnight, 0, 0);
            if (resetWuApi > DateTimeOffset.Now) resetWuApi = resetWuApi.AddDays(-1);
            oneWeekPast = DateTime.Now.AddDays(-7); // Seven days ago

            // Load up configuration settings
            try { MAX_A_DAY = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("MaxDay")); }
            catch { MAX_A_DAY = 500; }
            try { TILL_NEW = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Forcetime")); }
            catch { TILL_NEW = 300.0; }
            try { TILL_STALE = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Lifetime")); }
            catch { TILL_STALE = 30.0; }
            try { TILL_DEATH = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Deathtime")); }
            catch { TILL_DEATH = 360.0; }
            try { CACHE_ALIAS = RoleEnvironment.GetConfigurationSettingValue("CacheEndPoint"); }
            catch { CACHE_ALIAS = "Memcached"; }
            /**
             * Initialize the CACHE if needed.
             *  Note: To use the Increment/Decrement you need CREATE a memory location and store the original value 
             *  as a string.  Convert it back when reading. Of course, Increment uses ulong, just to be different.
             */
            cacheClient = WindowsAzureMemcachedHelpers.CreateDefaultClient(RoleEnvironment.CurrentRoleInstance.Role.Name, CACHE_ALIAS);
        }

        public XmlElement Build()
        {
            // Initialize
            string datefmtISO8601 = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
            DateTime beginning = new DateTime(2012, 6, 1);  // No data records exist before this date

            // Create a new xml document
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", null, null);
            doc.AppendChild(dec);
            XmlElement root = doc.CreateElement("Response");
            doc.AppendChild(root);

            // Create xml elements from the statistics
            
            // Configurations *******************************************************
            XmlElement e_Instance = doc.CreateElement("Instance");
            e_Instance.InnerText = RoleEnvironment.CurrentRoleInstance.Role.Name + "(" + RoleEnvironment.CurrentRoleInstance.Id.ToString() + ")";

            XmlElement e_InstanceStart = doc.CreateElement("InstanceStart");
            e_InstanceStart.InnerText = GetCacheData(RoleEnvironment.CurrentRoleInstance.Role.Name);

            XmlElement e_MachineTime = doc.CreateElement("MachineTime");
            e_MachineTime.InnerText = DateTime.Now.ToString(datefmtISO8601);

            XmlElement e_ConfigForceTime = doc.CreateElement("ConfigForceTime");
            e_ConfigForceTime.InnerText = RoleEnvironment.GetConfigurationSettingValue("Forcetime");

            XmlElement e_ConfigStaleTime = doc.CreateElement("ConfigStaleTime");
            e_ConfigStaleTime.InnerText = RoleEnvironment.GetConfigurationSettingValue("Lifetime");

            XmlElement e_ConfigDeathTime = doc.CreateElement("ConfigDeathTime");
            e_ConfigDeathTime.InnerText = RoleEnvironment.GetConfigurationSettingValue("Deathtime");

            XmlElement e_ConfigMaxWuDay = doc.CreateElement("ConfigMaxWuDay");
            e_ConfigMaxWuDay.InnerText = RoleEnvironment.GetConfigurationSettingValue("MaxDay");

            XmlElement e_ConfigMaxWuMinute = doc.CreateElement("ConfigMaxWuMinute");
            e_ConfigMaxWuMinute.InnerText = RoleEnvironment.GetConfigurationSettingValue("MaxMinute");

            XmlElement e_ConfigMaxCacheSize = doc.CreateElement("ConfigMaxCacheSize");
            e_ConfigMaxCacheSize.InnerText = RoleEnvironment.GetConfigurationSettingValue("CacheSize");

            // Counters *******************************************************
            XmlElement e_DayBegins = doc.CreateElement("DayBegins");
            e_DayBegins.InnerText = resetWuApi.ToString(datefmtISO8601);

            XmlElement e_WeekBegins = doc.CreateElement("WeekBegins");
            e_WeekBegins.InnerText = oneWeekPast.ToString(datefmtISO8601);

            // These queries against the azure storage take too long.
            //XmlElement e_StoredStations = doc.CreateElement("StoredStations");
            //CloudTableQuery<WStationTbl> stationCnt = WStationHelper.Read(deadage, now);
            //e_StoredStations.InnerText = stationCnt.ToArray<WStationTbl>().Length.ToString();

            // Populate the xml and return
            XmlElement stats = doc.CreateElement("Statistics");

            // Start up and configuration
            stats.AppendChild(e_Instance);
            stats.AppendChild(e_InstanceStart);
            stats.AppendChild(e_MachineTime);
            stats.AppendChild(e_ConfigForceTime);
            stats.AppendChild(e_ConfigStaleTime);
            stats.AppendChild(e_ConfigDeathTime);
            stats.AppendChild(e_ConfigMaxWuDay);
            stats.AppendChild(e_ConfigMaxWuMinute);
            stats.AppendChild(e_ConfigMaxCacheSize);
            stats.AppendChild(e_DayBegins);
            stats.AppendChild(e_WeekBegins);
            // WU-API
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, "WuApiCalls", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, "WuApiCalls", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, "WuApiCalls", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, "WuApiCalls", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WUAPI_WORKS, "WuApiCallsOK", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_CACHE_QNF, "CacheQNF", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WUAPI_QNF, "WuApiQNF", ref doc)); 
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_CACHE_BACKFAIL, "BackFailed", ref doc));
            // Logs 
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_LOG, "Log", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_LOG, "Log", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_LOG, "Log", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_LOG, "Log", ref doc));
            // Null 
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_RETURN_NULL, "Null", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_RETURN_NULL, "Null", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_RETURN_NULL, "Null", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_RETURN_NULL, "Null", ref doc));
            // Registration
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_SIGNUP, "Registration", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_SIGNUP, "Registration", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_SIGNUP, "Registration", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_SIGNUP, "Registration", ref doc));
            // Search
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_SEARCH, "Search", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_SEARCH, "Search", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_SEARCH, "Search", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_SEARCH, "Search", ref doc));
            // Basic
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_WSBASIC, "Station", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_WSBASIC, "Station", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSBASIC, "Station", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSBASIC, "Station", ref doc));
            // Slim
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_WSSMALL, "Slim", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_WSSMALL, "Slim", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSSMALL, "Slim", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSSMALL, "Slim", ref doc));
            // Detail 
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_WSDETAIL, "Detail", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_WSDETAIL, "Detail", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSDETAIL, "Detail", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSDETAIL, "Detail", ref doc));
            // Force
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_WSFORCE, "Force", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_WSFORCE, "Force", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSFORCE, "Force", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_WSFORCE, "Force", ref doc));
            // Queue
            stats.AppendChild(CountRange(COUNT_RANGE.TODAY, ServiceLedger.CNT_CODES.CNT_WT_QUEUE, "Queue", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.YESTERDAY, ServiceLedger.CNT_CODES.CNT_WT_QUEUE, "Queue", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.WEEK, ServiceLedger.CNT_CODES.CNT_WT_QUEUE, "Queue", ref doc));
            stats.AppendChild(CountRange(COUNT_RANGE.AVG_WEEK, ServiceLedger.CNT_CODES.CNT_WT_QUEUE, "Queue", ref doc));

            root.AppendChild(stats);

            return doc.DocumentElement;
        }

        private XmlElement CountRange(COUNT_RANGE range, ServiceLedger.CNT_CODES code, string name, ref XmlDocument doc)
        {
            string inner = "";
            string fullname = name;
            double average = 0;

            switch (range)
            {
                case COUNT_RANGE.TODAY:
                    inner = WCountDataHelper.Total(code, resetWuApi, DateTimeOffset.Now).ToString();
                    fullname += "Today";
                    break;
                case COUNT_RANGE.YESTERDAY:
                    inner = WCountDataHelper.Total(code, resetWuApi.AddDays(-1), resetWuApi).ToString();
                    fullname += "Yesterday";
                    break;
                case COUNT_RANGE.WEEK:
                    inner = WCountDataHelper.Total(code, oneWeekPast, DateTimeOffset.Now).ToString();
                    fullname += "Week";
                    break;
                case COUNT_RANGE.AVG_WEEK:
                    average = WCountDataHelper.Total(code, oneWeekPast, DateTimeOffset.Now) / 7;
                    inner = Math.Truncate(average).ToString();
                    fullname += "Average";
                    break;
                case COUNT_RANGE.UNKNOWN:
                default:
                    break;
            }

            XmlElement e_HoldXml = doc.CreateElement(fullname);
            e_HoldXml.InnerText = inner;
            return e_HoldXml;
        }
        

        /// <summary>
        /// Used to retrieve counters from the CACHE.
        /// </summary>
        private double GetCacheCounter(string key)
        {
            try { return Convert.ToDouble(GetCacheData(key)); }
            catch { return 0; }
        }
        private DateTime GetCacheTimestamp(string key)
        {
            try { return DateTime.Parse(GetCacheData(key)); }
            catch { return DateTime.Now; }
        }
        private string GetCacheData(string key)
        {
            try
            {
                object holdCnt;
                if (cacheClient.TryGet(key, out holdCnt))
                    return holdCnt.ToString();
                else
                    return "";
            }
            catch { return ""; }
        }
                

    }
}
