using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Configuration;
using System.Net;
using System.IO;
using WSHelpers;
using ThisMethod = System.Reflection.MethodInfo;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Coolftc.WTClimate
{
    /// <summary>
    /// The WStation class represent the weather station data built up from the WU data API.  The data element is xml
    /// which is easy to send back to the REST client and store in the cache container.
    /// </summary>
    public class WStation
    {
        /********************************************************************************************
        / Constants
         *
         * The Weather url uses the conditions/forecast10day/astronomy/almanac/alerts key words to get all the data we need.
         * See http://www.wunderground.com/weather/api/d/documentation.html for documentation.
         * NOTE: This data also exists in Climate.svc.cs of Web Service Interface.
         */
        //private static string GEOCODE = "geolookup/q/";
        private static string CLIMATE = "conditions/forecast10day/astronomy/almanac/alerts/q/";
        private static string DATA_NA = "-9999";

        /********************************************************************************************
        / Public Properties
         */
        public DateTime timestamp;      // The system time that the cached record was created.
        public XmlElement stationData;  // The xml stored in the cache

        /********************************************************************************************
        / Private Variable
         */
        private double m_newtime;       // The data is not new after this many seconds.
        private double m_lifetime;      // The data is stale after this many minutes.
        private double m_deathtime;     // The data is dead after this many minutes.

        /********************************************************************************************
        / Constructors
         */
        public WStation(double lifetime, double deathtime, double newtime)
        {
            timestamp = DateTime.Now;
            m_lifetime = lifetime;
            m_deathtime = deathtime;
            m_newtime = newtime;
        }

        public WStation(double lifetime, double deathtime, double newtime, XmlElement data)
        {
            timestamp = DateTime.Now;
            m_lifetime = lifetime;
            m_deathtime = deathtime;
            m_newtime = newtime;
            stationData = data;
        }

        public WStation(double lifetime, double deathtime, double newtime, XmlElement data, DateTime update)
        {
            timestamp = update;
            m_lifetime = lifetime;
            m_deathtime = deathtime;
            m_newtime = newtime;
            stationData = data;
        }

        public WStation(double lifetime, double deathtime, double newtime, string data, DateTime update)
        {
            timestamp = update;
            m_lifetime = lifetime;
            m_deathtime = deathtime;
            m_newtime = newtime;

            XmlDocument holdData = new XmlDocument();
            holdData.LoadXml(data);
            stationData = holdData.DocumentElement;
        }

        // This constructor expects "data" to be the output of RawStation().
        public WStation(double lifetime, double deathtime, double newtime, string data)
        {
            int pos = data.IndexOf("<");
            timestamp = DateTime.Parse(data.Substring(0, pos));
            m_lifetime = lifetime;
            m_deathtime = deathtime;
            m_newtime = newtime;

            XmlDocument holdData = new XmlDocument();
            holdData.LoadXml(data.Substring(pos));
            stationData = holdData.DocumentElement;
        }

        /********************************************************************************************
        / Public Methods
         */
        public Boolean isNotNew()
        {
            // Checks if the timestamp + newtime is less than the current time, in which case it has expired.
            return (timestamp.AddSeconds(m_newtime).CompareTo(DateTime.Now) < 0);
        }

        public Boolean isStale()
        {
            // Checks if the timestamp + lifetime is less than the current time, in which case it has expired.
            return (timestamp.AddMinutes(m_lifetime).CompareTo(DateTime.Now) < 0);
        }

        public Boolean isDead()
        {
            // Checks if the timestamp + deathtime is less than the current time, in which case it should not be used.
            return timestamp.AddMinutes(m_deathtime).CompareTo(DateTime.Now) < 0;
        }

        public Boolean Load(string station, double keyMax)
        {
            try
            {
                string path = CLIMATE + station;
                stationData = WebRestHelp.getRestWUAPI(path, keyMax);

                return (stationData != null);
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return false;
            }
        }

        /// <summary>
        /// This take the two important data items and concatenates them.  The timestamp is serialized into an ISO 8601 formatted
        /// date string.  This format can be parsed by the DateTime built-in parser (see constructor).  The timestamp is then placed
        /// in front of the xml element for easy separation later.
        /// </summary>
        public string RawStation()
        {
            return timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz") + stationData.OuterXml;
        }

        /// <summary>
        /// To help speed up the response time for the caller, this optional method allows the detail weather data to be returned
        /// without the forecast.  The rational is that for weather stations that are close together, the forecast data will be
        /// duplicated.  This gives the caller the option of foregoing the (significantly) large forecast data.
        /// </summary>
        public XmlElement DetailStation()
        {
            // This basically returns the station data without the <forecast> node.
            // We need to make a literal copy of the Station Data, or else the cached copy will lose its forecast node.
            XmlDocument holdXml = new XmlDocument();
            XmlNodeList xNodes;

            holdXml.LoadXml(stationData.OuterXml);
            xNodes = holdXml.GetElementsByTagName("forecast");

            for (int i = 0; i < xNodes.Count; i++)
            {
                xNodes[i].ParentNode.RemoveChild(xNodes[i]);
            }

            return holdXml.DocumentElement;
        }

        /// <summary>
        /// Since Azure charges by the byte, and the client mostly needs a small amount of data, use this function to whittle 
        /// down a "small" view of the data to just a few, required bytes.  Then the idea of a constantly updated client is reasonable.
        /// NOTE:  Initially this was part of object creation, but that did not always work, so now its just dynamically generated.
        /// </summary>
        public XmlElement SmallStation()
        {
            // The idea is to hunt and peck through the main xml to find just the minimum of what is needed.  
            // The goal here is 99.99% reduction in data size, since it cost money in azure to send bytes.
            XmlDocument holdXml = new XmlDocument();
            XmlNodeList xNodes;

            // Items needed: Temperature, Condition, Icon, Night or Day, Data stale, Alerts, Timezone, Humidity, DewPoint, Precipitation today
            // These items need to stay in the same order for backward compatibility.
            string temperature = DATA_NA;   // The WT will understand this as invalid
            string condition = "Clear";
            string icon = "clear";
            string night = "0"; // 0 = false
            string stale = "0"; // 0 = false
            string alert = "0"; // 0 = false
            string tzone = "America/Los_Angeles";
            string humid = DATA_NA;
            string dewpoint = DATA_NA;
            string precip = DATA_NA;

            // Most of the tags are unique, but do a little pruning to play it safe
            XmlNodeList currentObsNode = stationData.GetElementsByTagName("current_observation");
            XmlNodeList moonPhaseNode = stationData.GetElementsByTagName("moon_phase");
            XmlNodeList alertsNode = stationData.GetElementsByTagName("alert");

            // Sometimes the WU-API returns empty nodes, in which case we wish to keep the defaults
            if (currentObsNode.Count > 0)
            {
                holdXml.LoadXml(currentObsNode[0].OuterXml);
                xNodes = holdXml.DocumentElement.GetElementsByTagName("temp_f");
                if (xNodes.Count > 0) if(xNodes[0].InnerText.Length > 0) temperature = xNodes[0].InnerText;
                xNodes = holdXml.DocumentElement.GetElementsByTagName("weather");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) condition = xNodes[0].InnerText;
                xNodes = holdXml.DocumentElement.GetElementsByTagName("icon");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) icon = xNodes[0].InnerText;
                xNodes = holdXml.DocumentElement.GetElementsByTagName("local_tz_long");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) tzone = xNodes[0].InnerText;
                xNodes = holdXml.DocumentElement.GetElementsByTagName("relative_humidity");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) humid = xNodes[0].InnerText.Replace("%","");
                xNodes = holdXml.DocumentElement.GetElementsByTagName("dewpoint_f");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) dewpoint = xNodes[0].InnerText;
                xNodes = holdXml.DocumentElement.GetElementsByTagName("precip_today_in");
                if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) precip = xNodes[0].InnerText;
            }
            if (moonPhaseNode.Count > 0)
            {
                int CT = 1200; int SR = 0001; int SS = 2359;
                TimeSpan timeSinceUdt = DateTime.Now.Subtract(timestamp);

                holdXml.LoadXml(moonPhaseNode[0].OuterXml);
                // Current Time
                xNodes = holdXml.DocumentElement.GetElementsByTagName("current_time");
                if (xNodes.Count > 0) CT = GetRawTimeValue(xNodes[0].OuterXml, 1201);
                CT += Convert.ToInt32(timeSinceUdt.TotalMinutes);   // Update the current time for better night/day approximation.
                // Sunrise Time
                xNodes = holdXml.DocumentElement.GetElementsByTagName("sunrise");
                if (xNodes.Count > 0) SR = GetRawTimeValue(xNodes[0].OuterXml, 1);
                // Sunset Time
                xNodes = holdXml.DocumentElement.GetElementsByTagName("sunset");
                if (xNodes.Count > 0) SS = GetRawTimeValue(xNodes[0].OuterXml, 2359);

                if (CT < SR || CT > SS) night = "1";
            }
            if (alertsNode.Count > 0) alert = "1";
            if (isStale()) stale = "1"; 

            // Using <response> as the root node makes it consistent with the rest of the xml responses.
            string smallTxt = "<response><sl>" + temperature + "?" + condition + "?" + icon + "?" + night + "?" + stale + "?" + alert + "?" + tzone + "?" + humid + "?" + dewpoint + "?" + precip + "</sl></response>";
            XmlDocument small = new XmlDocument();
            small.LoadXml(smallTxt);
            return small.DocumentElement;
        }

        /********************************************************************************************
        / Private Methods
         */
        /// <summary>
        /// This is a little bit of a hack, since we know sunrise/sunset is not going to happen at noon
        /// or midnight, we ignore edge conditions.  Also assume the data supplied is in 24 hour format.
        /// </summary>
        private int GetRawTimeValue(string node, int backstop)
        {
            XmlDocument ali = new XmlDocument();
            XmlNodeList con;
            string holdTH = ""; string holdTM = ""; int rtn = backstop;

            ali.LoadXml(node);
            con = ali.DocumentElement.GetElementsByTagName("hour");
            if (con.Count > 0) holdTH = con[0].InnerText;
            con = ali.DocumentElement.GetElementsByTagName("minute");
            if (con.Count > 0) holdTM = con[0].InnerText;
            try { rtn = Convert.ToInt32(holdTH + holdTM); }
            catch { /* we have the backstop to return as default */ }

            return rtn;
        }

    }
}
