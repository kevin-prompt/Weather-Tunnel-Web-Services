using System;
using System.Linq;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Xml;
using System.Data.Services.Client;
using WSHelpers;

namespace Coolftc.WTClimate
{
    /// <summary>
    /// This class represents the data model of our Table.  It will be used to create the scheme and populate
    /// individual records.  The key is a combination of the Prepaq Name and the Destination Name.
    /// </summary>
    public class WPrepaqTbl : TableServiceEntity
    {
        public WPrepaqTbl()
        {
            PartitionKey = "";
            RowKey = "";     
        }
        public WPrepaqTbl(string prepaq, string name, string link)
        {
            PartitionKey = prepaq;  // All destinations are grouped into a prepaq.
            RowKey = name;          // Destination names are unique within a prepaq.
            Coordinates = link;     // Generally the latitude and longitude of the destination (in that order, comma separated).
        }
        public string Coordinates { get; set; }
    }

    /// <summary>
    /// This class supports certain constants, configuration values and static methods that make is easy to 
    /// perform NURD actions on the table.
    /// </summary>
    static public class WPrepaqHelper
    {
        // Per MSFT May 2009 - Due to a known performance issue with the ADO.NET Data Services client library, 
        // it is recommended that you use the table name for the class definition (which I have done here).
        static private string m_WPrepaqTblName = "WPrepaqTbl";
        static private string m_connectSettingName = "PrepaqTableConnection";
        static private Object HelperLock = new Object();

        static WPrepaqHelper()
        {
            // Check if TABLE exists and create if not.  Should be done rarely (e.g. first run or after a table delete), but when
            // it needs to be done, it really needs to be done.  This static constructor should be a reasonable compromise. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
            storageAccount.CreateCloudTableClient().CreateTableIfNotExist(WPrepaqTblName);
        }

        /// <summary>
        /// Put a Prepaq destination in the Table.  Will either create a new record or replace an existing one.
        /// Note: This requires a different version header: context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
        /// </summary>
        static public void Put(string prepaq, string name, string link)
        {
            lock (HelperLock)
            {
                try
                {
                    WPrepaqTbl entry = new WPrepaqTbl(prepaq, name, link);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
                    context.AttachTo(WPrepaqTblName, entry);
                    context.UpdateObject(entry);
                    context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Put", realMsg);
                }
            }
        }

        /// <summary>
        /// Store a new Destination in the Table.
        /// </summary>
        static public void Store(string prepaq, string name, string link)
        {
            lock (HelperLock)
            {
                try{
                WPrepaqTbl entry = new WPrepaqTbl(prepaq, name, link);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                context.AddObject(WPrepaqTblName, entry);
                context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Store", realMsg);
                }
            }
        }

        /// <summary>
        /// Store a multiple Destinations in the Table.
        /// </summary>
        /// <param name="destinations"></param>
        static public void Store(WPrepaqTbl[] destinations)
        {
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                for (int i = 0; i < destinations.Length; ++i)
                {
                    WPrepaqTbl entry = destinations[i];
                    context.AddObject(WPrepaqTblName, entry);
                }
                context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.Batch);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Store(Batch)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all Destinations for a Prepaq. Returns an IEnumeration that can be used to get the actual
        /// data. Use a "foreach" to read the actual data, and it will automatically only pull down the chuncks it needs.
        /// </summary>
        static public CloudTableQuery<WPrepaqTbl> Read(string prepaq)
        {
            lock (HelperLock)
            {
                try{
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    CloudTableQuery<WPrepaqTbl> itemsHold = (from entry in context.CreateQuery<WPrepaqTbl>(WPrepaqTblName)
                                                              where (entry.PartitionKey.Equals(prepaq))
                                                             select entry).AsTableServiceQuery<WPrepaqTbl>();
                    return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Read", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove a Destination from a Prepaq.
        /// </summary>
        static public void Delete(string prepaq, string destination)
        {
            lock (HelperLock)
            {
                try{
                WPrepaqTbl entry = new WPrepaqTbl(prepaq, destination, null);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();

                context.AttachTo(WPrepaqTblName, entry, "*"); // Ignore etag on db, which will not match default one.
                context.DeleteObject(entry);
                context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Delete", realMsg);
                }
            }
        }

        // Remove all Destinations in a Prepaq.
        static public void Delete(string prepaq)
        {
            CloudTableQuery<WPrepaqTbl> allPaqs = WPrepaqHelper.Read(prepaq);
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                foreach (WPrepaqTbl entry in allPaqs)
                {
                    context.Detach(entry);
                    context.AttachTo(WPrepaqTblName, entry, "*"); // Ignore etag on db, so it always get deleted.
                    context.DeleteObject(entry);
                }
                context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.Batch);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WPrepaqHelper.Delete(ByPrepaq)", realMsg);
                }
            }
        }

        static public string WPrepaqTblName { get { return m_WPrepaqTblName; } }
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
