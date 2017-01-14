using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Configuration;
using WSHelpers;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Concurrent;
using ThisMethod = System.Reflection.MethodInfo;
using System.Threading;
using Enyim.Caching;
using Microsoft.WindowsAzure.StorageClient;
using System.ServiceModel.Channels;

namespace Coolftc.WTClimate
{
    // Using the Namespace helps clean up the WSDL, AddressFilterMode lets you use http on Azure for the 
    // ws binding and ASP Compatibility gives you access to the HTTP headers.     
    [ServiceBehavior(Namespace = "http://coolftc.org/CoolftcWTClimate", AddressFilterMode = AddressFilterMode.Any)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Climate : IClimate
    {
        /********************************************************************************************
        / Constants
         *
         * The Weather url uses the conditions/forecast10day/astronomy/almanac/alerts key words to get all the data we need.
         * See http://www.wunderground.com/weather/api/d/documentation.html for documentation. 
         * NOTE: This data also exists in WStation.cs of the Worker Role.
         */
        private static string GEOCODE = "geolookup/q/";
        private static string CLIMATE = "conditions/forecast10day/astronomy/almanac/alerts/q/";
        private static int REG_BILLING = 9;
        private static ConcurrentDictionary<string, bool> tokens = new ConcurrentDictionary<string, bool>();
        private static Instrumentation Stats = new Instrumentation();
        private static MemcachedClient cacheClient = WindowsAzureMemcachedHelpers.CreateDefaultClient(RoleEnvironment.CurrentRoleInstance.Role.Name, Stats.CACHE_ALIAS);
        private static string ROLE_NAME = RoleEnvironment.CurrentRoleInstance.Role.Name + "(" + RoleEnvironment.CurrentRoleInstance.Id.ToString() + ")";
        private static string APPL_NAME = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + ".Climate";
        private enum ST_CACHE { ST_READY, ST_STALE, ST_DEAD, ST_NOTFOUND, ST_WEBROLE } 

        public string Version()
        {
            try
            {
                // Get the version out of the AssemblyInfo.cs file.
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " running on " + ROLE_NAME;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Register
         */
        public XmlElement GetRegistration(string ticket, string unique)
        {
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("unique", unique);

                // Empty xml document
                XmlDocument reg = new XmlDocument();
                XmlDeclaration dec = reg.CreateXmlDeclaration("1.0", null, null);
                reg.AppendChild(dec);
                XmlElement root = reg.CreateElement("response");
                reg.AppendChild(root);

                // Generate and Save the key+unique data as the ticket
                string key = LoadRegKey(unique);

                // Populate document
                XmlElement id = reg.CreateElement("wt_id");
                id.InnerText = key;
                root.AppendChild(id);

                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_SIGNUP);
                ServiceLedger.Audit(new ClassAud(key, ROLE_NAME, buildSource(ticket), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, REG_BILLING));
                return reg.DocumentElement;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public static void FlushToken(string key)
        {
            tokens.AddOrUpdate(key, false, (tik, off) => false);
        }

        /********************************************************************************************
         * Climate
         * There are different response types of weather data, depending on what chunk of data is needed
         * and/or how old the data can be.
         * SL = Slim is used to return a small amount of data
         * DL = Detail is used to return a medium amount of data
         * FR = Force is used to return a medium amount of data that is very recent.
         * Without a code suffix, just return the whole data point.
         */
        public XmlElement GetWeatherSL(string ticket, string station)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("station", station);
                string [] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_WSSMALL);
            #endregion

                // First see if we can find the station's weather in the Cache.  If it does not exist there, 
                // do a direct read from the WU-API, update the Cache and send the data back.
                ST_CACHE rtn = ST_CACHE.ST_NOTFOUND;
                WStation data = GetCacheStation(station, out rtn);

                // If no Cache Hit, go get the real thing.
                if (data == null)
                {
                    string path = CLIMATE + station;
                    XmlElement stData = WebRestHelp.getRestWUAPI(path, Stats.MAX_A_DAY);
                    if (stData != null) data = NewStation(station, stData);
                }

                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, (int)rtn));
                if (data == null)
                    return WebRestHelp.NonResponse();
                else
                    return data.SmallStation();

            #region Web Service Exception Catch
            }
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "::Parameter= " + station + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public XmlElement GetWeatherDL(string ticket, string station)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("station", station);
                string[] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_WSDETAIL);
            #endregion

                // First see if we can find the station's weather in the Cache.  If it does not exist there, 
                // do a direct read from the WU-API, update the Cache and send the data back.
                ST_CACHE rtn = ST_CACHE.ST_NOTFOUND;
                WStation data = GetCacheStation(station, out rtn);

                // If no Cache Hit, go get the real thing.
                if (data == null)
                {
                    string path = CLIMATE + station;
                    XmlElement stData = WebRestHelp.getRestWUAPI(path, Stats.MAX_A_DAY);
                    if (stData != null) data = NewStation(station, stData);
                }

                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, (int)rtn));
                if (data == null)
                   return WebRestHelp.NonResponse();
                else
                   return data.DetailStation();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "::Parameter= " + station + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public XmlElement GetWeatherFR(string ticket, string station)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("station", station);
                string[] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_WSFORCE);
            #endregion

                // First see if we can find the station's weather in the Cache.  If it does not exist there, 
                // do a direct read from the WU-API, update the Cache and send the data back.
                ST_CACHE rtn = ST_CACHE.ST_NOTFOUND;
                WStation data = GetCacheStation(station, out rtn);

                // If no Cache Hit or not new enough, go get the real thing.
                if (data == null || data.isNotNew())
                {
                    string path = CLIMATE + station;
                    XmlElement stData = WebRestHelp.getRestWUAPI(path, Stats.MAX_A_DAY);
                    if (stData != null) data = NewStation(station, stData);
                }

                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, (int)rtn));

                // For now lets save the return trip time and just use this method to update the cache (and not send any data back).
                // Empty xml document
                return WebRestHelp.NonResponse();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "::Parameter= " + station + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public XmlElement GetWeather(string ticket, string station)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("station", station);
                string[] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_WSBASIC);
            #endregion

                // First see if we can find the station's weather in the Cache.  If it does not exist there, 
                // do a direct read from the WU-API, update the Cache and send the data back.
                ST_CACHE rtn = ST_CACHE.ST_NOTFOUND;
                WStation data = GetCacheStation(station, out rtn);

                // If no Cache Hit, go get the real thing.
                if (data == null)
                {
                    string path = CLIMATE + station;
                    XmlElement stData = WebRestHelp.getRestWUAPI(path, Stats.MAX_A_DAY);
                    if (stData != null) data = NewStation(station, stData);
                }

                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, (int)rtn));
                if (data == null)
                    return WebRestHelp.NonResponse();
                else
                    return data.stationData;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "::Parameter= " + station + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Location information
         */
        public XmlElement GetLocation(string ticket, string link)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("link", link);
                string[] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_SEARCH); 
            #endregion

                string path = GEOCODE + link;
                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, (int)ST_CACHE.ST_NOTFOUND));
                return WebRestHelp.getRestWUAPI(path, Stats.MAX_A_DAY);
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "::Parameter= " + link + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public XmlElement GetPrepaqList(string ticket, string prepaq)
        {
            #region Web Service Security
            try
            {
                // Audit the parameters.
                string holdParms = ServiceLedger.Tag("prepaq", prepaq);
                string[] exTicket = CheckLogTicket(ticket);
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_PREPAQ);
            #endregion

                // Empty xml document
                XmlDocument destList = new XmlDocument();
                XmlDeclaration dec = destList.CreateXmlDeclaration("1.0", null, null);
                destList.AppendChild(dec);
                XmlElement root = destList.CreateElement("response");
                destList.AppendChild(root);

                // Get the data
                CloudTableQuery<WPrepaqTbl> items = WPrepaqHelper.Read(prepaq);

                // Populate document
                foreach (WPrepaqTbl item in items)
                {
                    XmlElement place = destList.CreateElement("place");
                    XmlElement name = destList.CreateElement("dest");
                    name.InnerText = item.RowKey;
                    XmlElement link = destList.CreateElement("link");
                    link.InnerText = item.Coordinates;
                    place.AppendChild(name);
                    place.AppendChild(link);
                    root.AppendChild(place);
                }

                ServiceLedger.Audit(new ClassAud(exTicket[0], ROLE_NAME, buildSource(exTicket[1]), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return destList.DocumentElement;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc, ticket);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc, ticket);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Private Helpers
         */
        private string [] CheckLogTicket(string xticket)
        {
            // ticket = GUID_Version (e.g. 064469bd-9b2a-442d-9c9a-6427a70ca5eaAndroidWTv1.2)
            bool holdact = false;
            string[] ticket = new string[2];
            ticket[0] = xticket.Substring(0, 36);
            ticket[1] = xticket.Length > 36 ? xticket.Substring(36) : "";
            
            if (tokens.ContainsKey(ticket[0]))
            {
                tokens.TryGetValue(ticket[0], out holdact);
                if (holdact) return ticket;
            }
            WRegisterTbl regIDs = WRegisterHelper.Read(ticket[0]);
            if (regIDs != null)
            {
                holdact = Convert.ToBoolean(regIDs.RowKey);
                if (holdact) { tokens.AddOrUpdate(ticket[0], true, (key, old) => true); return ticket; }
            }

            throw new ClassExp(ClassExp.EXP_CODES.EXP_NOMATCH, this.ToString());
        }

        // Store the generated key plus the unique date in both table and queue storage.
        private string LoadRegKey(string unique)
        {
            string key = Guid.NewGuid().ToString();
            WRegisterHelper.Store(key, unique);
            tokens.AddOrUpdate(key, true, (tik, off) => true);
            return key;
        }

        /**
         * This method is called after a call has been made to the WU-API for the data.  This adds it
         * to the Cache & Storage.
         */
        private WStation NewStation(string station, XmlElement data)
        {
            // Place the new station data in the Cache and Storage
            if (data == null)
                throw new ClassExp(ClassExp.EXP_CODES.EXP_NODATA, RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name, "The station data cannot be NULL - " + station);
            WStation holdStation = new WStation(Stats.TILL_STALE, Stats.TILL_DEATH, Stats.TILL_NEW, data);
            bool rtn = cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Set, station, holdStation.RawStation());
            // This PUT will only work on the real platform, not on the Emulator. 
            try { WStationHelper.Put(station, holdStation.stationData.OuterXml, holdStation.timestamp); }
            catch (ClassExp kerr)
            { // Not the end of the world if it fails
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                //if (!kerr.code == ClassExp.EXP_CODES.EXP_TS_SIZE)
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
            }
            catch (Exception e)
            { // Not the end of the world if it fails
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "(Station=" + station + ") --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
            }

            return holdStation;
        }

        /**
         * This will check the Cache to see if the station is available.  There are 4 possible conditions
         * that a station can be in: 1) Not in Cache (not found), 2) In Cache but very old (dead), 
         * 3) In Cache and old (stale), 4) In Cache and current (found).  The status will be returned with
         * either the station data (found, stale) or null (not found, dead).
         */
        private WStation GetCacheStation(string station, out ST_CACHE status)
        {
            object holdStationCACHE;
            if (cacheClient.TryGet(station, out holdStationCACHE))
            {
                WStation holdStation = new WStation(Stats.TILL_STALE, Stats.TILL_DEATH, Stats.TILL_NEW, (string)holdStationCACHE);
                if (holdStation.isDead())        // A null value is returned. Will be updated by Climate.
                {
                    status = ST_CACHE.ST_DEAD;
                    return null;
                }

                if (holdStation.isStale())      // Return but queue it to perform refresh.
                {
                    status = ST_CACHE.ST_STALE;
                    CloudQueueMessage message = new CloudQueueMessage(station);
                    StaleQueueHelper.StaleQueue.AddMessage(message);
                    return holdStation;
                }

                status = ST_CACHE.ST_READY;
                return holdStation; // Return current data.
            }
            else
            {
                status = ST_CACHE.ST_NOTFOUND;
                return null;
            }

        }

        /// <summary>
        /// Used to retrieve counters from the CACHE.
        /// </summary>
        private double GetCacheCounter(string key)
        {
            try
            {
                object holdCnt;
                if (cacheClient.TryGet(key, out holdCnt))
                    return Convert.ToDouble(holdCnt.ToString());
                else
                    return 0;
            }
            catch { return 0; }
        }

        private string buildSource(string ver)
        {
            if (ver.Length.Equals(0)) ver = "unknown";
            string holdParms = ServiceLedger.Tag("ver", ver);
            holdParms += ServiceLedger.Tag("ip", getRemoteIP());
            return holdParms;
        }

        /// <summary>
        /// Get the incoming IP address
        /// </summary>
        private string getRemoteIP()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties msgProp = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty epProp = msgProp[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            return epProp != null ? epProp.Address : "No IP";
        }

    }
}