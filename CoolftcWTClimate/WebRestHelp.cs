using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using WSHelpers;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections.Concurrent;

namespace Coolftc.WTClimate
{
    public static class WebRestHelp
    {
        /// <summary>
        /// The query not found (qnf) collection stores stations that have been flagged as no longer
        /// available from the WU-API.  The private stations are dropped from the valid list for a 
        /// number reasons.  They can always come back, so this list should be periodically cleared.
        /// </summary>
        public static ConcurrentDictionary<string, DateTime> qnf = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// The usual action is to use the primary key.  If that key overruns by a small amount, it can be 
        /// supplemented by a list of other keys.  The backup keys would generally be for slight overruns.
        /// The supplemental list expects each key to have a fixed max value, and they are used sequentially.
        /// </summary>
        private static string getWuiKey(bool useBaseKey)
        {
            string METHOD_NAME = "getWuiKey";
            try
            {
                char[] DELIMITER = { '?' };
                string wuiKey = "";
                string[] fallbackKeys;
                int offset = 0;
                if (useBaseKey)
                {
                    wuiKey = RoleEnvironment.GetConfigurationSettingValue("WUIkey");
                }
                else
                {
                    fallbackKeys = RoleEnvironment.GetConfigurationSettingValue("WUIkeyFailKeys").Split(DELIMITER, StringSplitOptions.RemoveEmptyEntries);
                    offset = DateTime.Now.Second % fallbackKeys.Count();
                    wuiKey = fallbackKeys[offset];
                    //ServiceLedger.Entry(17303, ServiceLedger.SEV_CODES.SEV_INFO, "Key = " + wuiKey, RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME);
                }
                if (!wuiKey.LastIndexOf("/").Equals(wuiKey.Length - 1)) wuiKey += "/";
                return wuiKey;
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return null;
            }
        }

        private static Uri makeEndpointWU(string path, string wuiKey)
        {
            string METHOD_NAME = "makeEndpointWU";
            try
            {
                string endpoint = RoleEnvironment.GetConfigurationSettingValue("WUIendpoint");
                if (!endpoint.LastIndexOf("/").Equals(endpoint.Length - 1)) endpoint += "/";
                string wuiFormat = RoleEnvironment.GetConfigurationSettingValue("WUIformat");
                if (!wuiFormat.Substring(0, 1).Equals(".")) wuiFormat = "." + wuiFormat;
                return new Uri(endpoint + wuiKey + path + wuiFormat);
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME + "::Parameter= " + path + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return null;
            }
        }

        /// <summary>
        /// This method is used to call REST based web services from WU-API. In all cases, the WU - API should 
        /// return xml data with a <response> node.  If that node is not found, or if it indicates there is no
        /// data, there is some problem and alternative data should be returned. FYI. Often the WU will return 
        /// a web page when their API is not working.
        /// </summary>
        public static XmlElement getRestWUAPI(string path, double keyMax)
        {
            string METHOD_NAME = "getRestWUAPI";
            XmlDocument doc = new XmlDocument();
            bool useBaseKey = false;
            DateTime qnfAge;
            Uri url = null;

            try
            {
                if (keyMax > 0)
                {
                    TimeZoneInfo NYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    int eastCoastMidnight = Math.Abs(NYTimeZone.BaseUtcOffset.Hours);
                    DateTimeOffset resetWU = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, eastCoastMidnight, 0, 0);
                    if (resetWU > DateTimeOffset.Now) resetWU = resetWU.AddDays(-1);
                    useBaseKey = WCountDataHelper.Total(ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, resetWU, DateTimeOffset.Now) <= keyMax;
                }
                url = makeEndpointWU(path, getWuiKey(useBaseKey));
                if (url == null)
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_NODATA, METHOD_NAME, "URL creation failed.");
                if (qnf.TryGetValue(path, out qnfAge))
                {
                    if (qnfAge < DateTime.Now.AddDays(-1)) // lets clear this out every so often. 
                    {
                        qnf.TryRemove(path, out qnfAge);
                        ServiceLedger.Entry(18000, ServiceLedger.SEV_CODES.SEV_INFO, "Clearing the Query Not Found list of item " + path, RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME);
                    }
                    else
                    {
                        WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_CACHE_QNF);
                        return null;    // return quietly
                    }
                }

                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS);
                HttpWebRequest wsRequ = WebRequest.Create(url) as HttpWebRequest;
                HttpWebResponse wsResp = wsRequ.GetResponse() as HttpWebResponse;
                Stream data = wsResp.GetResponseStream();
                doc.Load(data);
                data.Close();
                wsResp.Close();

                // Mostly, the xml parser will throw before we get here if the WU is down, as it will not like the HTML that
                // the WU-API returns when it is down.  This adds final checks for valid data just in case it returns xml. 

                // Check that the standard WU-API "response" is the root node.
                XmlNode isResponse = doc.SelectSingleNode("response");
                if (isResponse == null)
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_NODATA, METHOD_NAME, url.ToString() + " -- Node response not found.");
                // Check that we did not get some WU-API handled error, typically station not found (usually they come back).
                XmlNodeList errNodes = doc.GetElementsByTagName("error");
                if (errNodes.Count > 0)
                {
                    XmlDocument holdXml = new XmlDocument();
                    XmlNodeList xNodes;
                    string reason = "unknown reason";
                    holdXml.LoadXml(errNodes[0].OuterXml);
                    xNodes = holdXml.DocumentElement.GetElementsByTagName("type");
                    if (xNodes.Count > 0) if (xNodes[0].InnerText.Length > 0) reason = xNodes[0].InnerText;
                    if (reason.Equals("querynotfound") || reason.Equals("Station:OFFLINE"))
                    {
                        qnf.TryAdd(path, DateTime.Now);  // cache these to minimize WU-API calls for bad stations.
                        WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WUAPI_QNF);
                        // There are a lot of these, and it is not a problem with my code, so I do not want to fill up the log.  
                        return null;
                    }
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_WEB_NOMATCH, METHOD_NAME, url.ToString() + " -- " + reason);
                }

                // Everything OK
                WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WUAPI_WORKS);
                return doc.DocumentElement;

            }
            catch (WebException e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME + "::Parameter= " + path + " -status-> " + e.Status.ToString();
                if (e.Response != null) holdSrc += " -Response Code-> " + ((HttpWebResponse)e.Response).StatusCode.ToString();
                ServiceLedger.Entry(17301, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return null;
            }
            catch (ClassExp e)
            {
                ServiceLedger.Entry(e.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.codeDesc(), e.codeSource);
                return null;
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + METHOD_NAME + "::Parameter= " + path + " --" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return null;
            }
        }

        /// <summary>
        /// Nice to have empty but valid response.
        /// </summary>
        public static XmlElement NonResponse()
        {
            XmlDocument nonresponse = new XmlDocument();
            XmlDeclaration dec = nonresponse.CreateXmlDeclaration("1.0", null, null);
            nonresponse.AppendChild(dec);
            XmlElement root = nonresponse.CreateElement("response");
            nonresponse.AppendChild(root);
            WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_RETURN_NULL);
            return nonresponse.DocumentElement;
        }

    }
}