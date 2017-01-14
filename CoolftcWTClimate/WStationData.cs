using System;
using System.Linq;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Xml;
using WSHelpers;


namespace Coolftc.WTClimate
{
    /// <summary>
    /// This class represents the data model of our Table.  It will be used to create the scheme and populate
    /// individual records.  The key is a combination of the Application Name and the Station.  Since Station
    /// is a key of the hash table, it provides uniqueness.  The Report is the weather data.  The data that can
    /// be stored in a Table record is basically a string, so we need to convert the incoming xml into a string.
    /// It is limited to 64KB, which should be enough given the current data stored about weather conditions.
    /// </summary>
    public class WStationTbl : TableServiceEntity
    {
        public WStationTbl()
        {
            PartitionKey = WStationHelper.ApplicationName;
            RowKey = "";        // This will hold the station name.
        }
        public WStationTbl(string station, string report, DateTime update)
        {
            PartitionKey = WStationHelper.ApplicationName;
            RowKey = station;   // This will hold the station name.
            Update = update;
            Report = report;
        }
        public DateTime Update { get; set; }
        public string Report { get; set; }
    }

    /// <summary>
    /// This class supports certain constants, configuration values and static methods that make is easy to 
    /// perform NURD actions on the table.
    /// </summary>
    static public class WStationHelper
    {
        // Per MSFT May 2009 - Due to a known performance issue with the ADO.NET Data Services client library, 
        // it is recommended that you use the table name for the class definition (which I have done here).
        static private string m_WStationTblName = "WStationTbl";
        static private string m_AppName = "UnknownApplication";
        static private string m_connectSettingName = "CacheTableConnection";
        static private Object HelperLock = new Object();

        static WStationHelper()
        {
            try { m_AppName = RoleEnvironment.GetConfigurationSettingValue("ApplicationName"); }
            catch { m_AppName = "UnknownApplication"; }
            // Check if TABLE exists and create if not.  Should be done rarely (e.g. first run or after a table delete), but when
            // it needs to be done, it really needs to be done.  This static constructor should be a reasonable compromise. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
            storageAccount.CreateCloudTableClient().CreateTableIfNotExist(WStationTblName);
        }

        /// <summary>
        /// Put a Station in the Table.  Will either create a new record or replace an existing one.
        /// Note: This requires a different version header: context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
        /// Added size verification.  Table can only store 64K bytes, so reject with specific error if too big. For the WU-API data
        /// the Alerts push the string size over 32K characters, and it requires Unicode, making the overall value > 64KB.  Better to
        /// just skip saving that as it should be a small percentage of overall data.
        /// </summary>
        static public void Put(string station, string report, DateTime timestamp)
        {
            lock (HelperLock)
            {
                if ((report.Length * sizeof(char)) > (64 * 1024))
                {
                    WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_CACHE_BACKFAIL);
                    string size = (report.Length * sizeof(char)).ToString();
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_SIZE, "WStationHelper.Put", "Station: " + station + " is " + size + " bytes");
                }
                try
                {
                    WStationTbl entry = new WStationTbl(station, report, timestamp);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
                    context.AttachTo(WStationTblName, entry);
                    context.UpdateObject(entry);
                    context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_CACHE_BACKFAIL);
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Put", realMsg);
                }
            }
        }

        /// <summary>
        /// Store a new Station in the Table.
        /// </summary>
        static public void Store(string station, string report, DateTime timestamp)
        {
            lock (HelperLock)
            {
                try{
                WStationTbl entry = new WStationTbl(station, report, timestamp);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                context.AddObject(WStationTblName, entry);
                context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Store", realMsg);
                }
            }
        }

        /// <summary>
        /// Read a specific station, based on its name.  Returns null if the station does not exist.
        /// </summary>
        static public WStationTbl Read(string station)
        {
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                WStationTbl item = (from entry in context.CreateQuery<WStationTbl>(WStationTblName)
                                    where entry.PartitionKey.Equals(WStationHelper.ApplicationName) && entry.RowKey.Equals(station)
                                    select entry).FirstOrDefault();
                return item;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Read(ByKey)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all stations that fall within a given date. Returns an IEnumeration that can be used to get the actual
        /// data. Use a "foreach" to read the actual data, and it will automatically only pull down the chunks it needs.
        /// Note: The DateTime Min and Max dates are not valid inputs and will cause an exception.
        /// </summary>
        static public CloudTableQuery<WStationTbl> Read(DateTime start, DateTime end)
        {
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                CloudTableQuery<WStationTbl> itemsHold = (from entry in context.CreateQuery<WStationTbl>(WStationTblName)
                                                          where (entry.Timestamp > start && entry.Timestamp < end)
                                                          select entry).AsTableServiceQuery<WStationTbl>();
                return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Read(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all Stations. Returns an IEnumeration that can be used to get the actual data. Use a "foreach" to read the
        /// actual data, and it will automatically only pull down the chunks it needs.
        /// </summary>
        static public CloudTableQuery<WStationTbl> Read()
        {
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                CloudTableQuery<WStationTbl> itemsHold = (from entry in context.CreateQuery<WStationTbl>(WStationTblName)
                                                          where (entry.PartitionKey.Equals(WStationHelper.ApplicationName))
                                                          select entry).AsTableServiceQuery<WStationTbl>();
                return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Read", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove a Station.
        /// </summary>
        static public void Delete(string key)
        {
            lock (HelperLock)
            {
                try
                {
                    WStationTbl entry = new WStationTbl(key, null, DateTime.Now);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.AttachTo(WStationTblName, entry, "*"); // Ignore etag on db, which will not match default one.
                    context.DeleteObject(entry);
                    context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Delete", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove any Stations in the date range.
        /// Removed "Batch" on save because the size of the request was getting too big (over 4MB).
        /// </summary>
        static public void Delete(DateTime start, DateTime end)
        {
            CloudTableQuery<WStationTbl> backing = WStationHelper.Read(start, end);
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                foreach (WStationTbl entry in backing)
                {
                    context.Detach(entry);
                    context.AttachTo(WStationTblName, entry, "*"); // Ignore etag on db, so it always get deleted.
                    context.DeleteObject(entry);
                    context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.ContinueOnError);
                }
                
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Delete(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove all Stations.
        /// </summary>
        static public void Delete()
        {
            CloudTableQuery<WStationTbl> backing = WStationHelper.Read();
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                foreach (WStationTbl entry in backing)
                {
                    context.Detach(entry);
                    context.AttachTo(WStationTblName, entry, "*"); // Ignore etag on db, so it always get deleted.
                    context.DeleteObject(entry);
                }
                context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.Batch);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WStationHelper.Delete", realMsg);
                }
            }
        }

        static public string ApplicationName  { get { return m_AppName; } }
        static public string WStationTblName  { get { return m_WStationTblName; } }
        static public string CONNECT_SET_NAME { get { return m_connectSettingName; } }

        static private string parseTSErr(string msg)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(msg);
                string code = xml.GetElementsByTagName("code")[0].InnerText;
                string mess = xml.GetElementsByTagName("message")[0].InnerText;
                return code + "?" + mess;
            }
            catch { return "No further Info."; }
        }

    }
}
