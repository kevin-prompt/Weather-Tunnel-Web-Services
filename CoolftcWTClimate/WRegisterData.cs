using System;
using System.Linq;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Xml;
using WSHelpers;
using System.Data.Services.Client;

namespace Coolftc.WTClimate
{
    /// <summary>
    /// This class represents the data model of our Table.  It will be used to create the scheme and populate
    /// individual records.  The key is a combination of the ticket and if it is valid.  Since the ticket is 
    /// a GUID, it will provide uniqueness.  Having the valid boolean lets us manage the data a little more
    /// effectively.  The Device data is a unique piece of information from the device, e.g. some local id.
    /// </summary>
    public class WRegisterTbl : TableServiceEntity
    {
        public WRegisterTbl()
        {
            PartitionKey = "";
            RowKey = "";
        }
        public WRegisterTbl(string ticket, string unique)
        {
            PartitionKey = ticket;      // All destinations are grouped into a prepaq.
            RowKey = true.ToString();   // Is this valid.
            Device = unique;            // Generally the latitude and longitude of the destination.
        }
        public WRegisterTbl(string ticket, string unique, bool valid)
        {
            PartitionKey = ticket;      // All destinations are grouped into a prepaq.
            RowKey = valid.ToString();  // Is this valid.
            Device = unique;            // Generally the latitude and longitude of the destination.
        }
        public WRegisterTbl(string ticket, string unique, string valid)
        {
            PartitionKey = ticket;      // All destinations are grouped into a prepaq.
            RowKey = valid;             // Is this valid.
            Device = unique;            // Generally the latitude and longitude of the destination.
        }
        public string Device { get; set; }
    }

    /// <summary>
    /// This class supports certain constants, configuration values and static methods that make is easy to 
    /// perform NURD actions on the table.
    /// </summary>
    static public class WRegisterHelper
    {
        // Per MSFT May 2009 - Due to a known performance issue with the ADO.NET Data Services client library, 
        // it is recommended that you use the table name for the class definition (which I have done here).
        static private string m_WRegisterTblName = "WRegisterTbl";
        static private string m_connectSettingName = "RegisterTableConnection";
        static private Object HelperLock = new Object();

        static WRegisterHelper()
        {
            // Check if TABLE exists and create if not.  Should be done rarely (e.g. first run or after a table delete), but when
            // it needs to be done, it really needs to be done.  This static constructor should be a reasonable compromise. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
            storageAccount.CreateCloudTableClient().CreateTableIfNotExist(WRegisterTblName);
        }

        /// <summary>
        /// Put a Registration in the Table.  Will either create a new record or replace an existing one.
        /// Note: This requires a different version header: context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
        /// </summary>
        static public void Put(string ticket, string unique, bool act)
        {
            lock (HelperLock)
            {
                try
                {
                    WRegisterTbl entry = new WRegisterTbl(ticket, unique, act);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.SendingRequest += (_, e) => e.RequestHeaders["x-ms-version"] = "2011-08-18";
                    context.AttachTo(WRegisterTblName, entry);
                    context.UpdateObject(entry);
                    context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Put", realMsg);
                }
            }
        }

        /// <summary>
        /// Store a new Registration in the Table.
        /// </summary>
        static public void Store(string ticket, string unique) { Store(ticket, unique, true); }
        static public void Store(string ticket, string unique, bool act)
        {
            lock (HelperLock)
            {
                try{
                WRegisterTbl entry = new WRegisterTbl(ticket, unique, act);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                context.AddObject(WRegisterTblName, entry);
                context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Store", realMsg);
                }
            }
        }

        /// <summary>
        /// Read a specific ticket from the Registration. Returns null if the registration does not exist.
        /// </summary>
        static public WRegisterTbl Read(string ticket)
        {
            lock (HelperLock)
            {
                try{
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                TableServiceContext context = tableClient.GetDataServiceContext();
                WRegisterTbl item = (from entry in context.CreateQuery<WRegisterTbl>(WRegisterTblName)
                                    where entry.PartitionKey.Equals(ticket) select entry).FirstOrDefault();
                return item;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Read(ByKey)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all Registrations that fall within a given date. Returns an IEnumeration that can be used to get the actual
        /// data. Use a "foreach" to read the actual data, and it will automatically only pull down the chunks it needs.
        /// Note: The DateTime Min and Max dates are not valid inputs and will cause an exception.
        /// </summary>
        static public CloudTableQuery<WRegisterTbl> Read(DateTime start, DateTime end)
        {
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    CloudTableQuery<WRegisterTbl> itemsHold = (from entry in context.CreateQuery<WRegisterTbl>(WRegisterTblName)
                                                              where (entry.Timestamp > start && entry.Timestamp < end)
                                                               select entry).AsTableServiceQuery<WRegisterTbl>();
                    return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Read(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all Registrations. Returns an IEnumeration that can be used to get the actual data. Use a "foreach" to read the
        /// actual data, and it will automatically only pull down the chunks it needs.
        /// </summary>
        static public CloudTableQuery<WRegisterTbl> Read()
        {
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    CloudTableQuery<WRegisterTbl> itemsHold = (from entry in context.CreateQuery<WRegisterTbl>(WRegisterTblName)
                                                               select entry).AsTableServiceQuery<WRegisterTbl>();
                    return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Read", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove a Registration.
        /// </summary>
        static public void Delete(string key, string active)
        {
            lock (HelperLock)
            {
                try
                {
                    WRegisterTbl entry = new WRegisterTbl(key, null, active);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.AttachTo(WRegisterTblName, entry, "*"); // Ignore etag on db, which will not match default one.
                    context.DeleteObject(entry);
                    context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "WRegisterHelper.Delete", realMsg);
                }

            }
        }

        static public string WRegisterTblName { get { return m_WRegisterTblName; } }
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
