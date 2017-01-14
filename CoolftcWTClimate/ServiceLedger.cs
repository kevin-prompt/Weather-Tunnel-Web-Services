using System;
using System.Linq;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Data.SqlClient;
using System.Xml;

/* Requires reference to "System.Data.Services.Client" and a connection to the table: CloudTableConnection.
 *  Need to provide two keys in the role settings for logging.
 *   <!-- This is the name of the application. -->
 *   name="ApplicationName" value="Application Name"
 *   <!-- Set the Ledger Log recording level. Use -1 to log all.-->
 *   name="All_LedgerLevel" value="-1"
 *  Need to provide two keys in the role setting for auditing.
 *   <!-- Set the Region to allow the proper SQL database settings take hold. -->
 *   name="Region" value="PROD | DEVO"
 *   <!-- This is the password for the SQL database. -->
 *   name="SQL_Table" value="password"
 */

namespace WSHelpers
{
    /// <summary>
    /// This class represents the data model of our Table.  It will be used to create the scheme and populate
    /// individual records.  Note the key can be completely determined internally because the Log is unique
    /// to each application and entries are time ordered.
    /// </summary>
    public class LogLedger : TableServiceEntity
    {
        public LogLedger()
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = String.Format("{0:10}", (DateTime.MaxValue.Ticks - DateTime.Now.Ticks));
        }
        public LogLedger(string key)
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = key;
            Severity = 0;
            Message = "";
        }
        public LogLedger(int severity, string message)
        {
            PartitionKey = ServiceLedger.ApplicationName;
            RowKey = String.Format("{0:10}", (DateTime.MaxValue.Ticks - DateTime.Now.Ticks));
            Severity = severity;
            Message = message;
        }
        public int Severity { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// This class supports certain constants, configuration values and static methods that make is easy to 
    /// place an entry into the log table and delete an entry.  Note that both entry and delete only apply
    /// to the records of this application.  This is not a GLOBAL delete, each application supports its own delete.
    /// Creating new Audit records in the SQL table is also supported in this class.
    /// </summary>
    static public class ServiceLedger
    {
        public enum SEV_CODES
        {
            SEV_EXCEPTION = 1,    // Serious Problem
            SEV_ALERT = 3,        // Item to take note of
            SEV_INFO = 5,         // Informational message
            SEV_DEBUG = 7         // Debug messages
        };
        // Per MSFT May 2009 - Due to a known performance issue with the ADO.NET Data Services client library, 
        // it is recommended that you use the table name for the class definition (which I have done here).
        static private string m_LogTableName = "LogLedger";
        static private string m_AppName = "UnknownApplication";
        static private string m_connectSettingName = "CloudTableConnection";
        static private int MAX_SEV = -1;
        static private Object HelperLock = new Object();

        static ServiceLedger()
        {
            try
            {
                MAX_SEV = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("All_LedgerLevel"));
            }
            catch { MAX_SEV = -1; }
            try
            {
                m_AppName = RoleEnvironment.GetConfigurationSettingValue("ApplicationName");
            }
            catch { m_AppName = "UnknownApplication"; }
            // Check if TABLE exists and create if not.  Should be done rarely (e.g. first run or after a table delete), but when
            // it needs to be done, it really needs to be done.  This static constructor should be a reasonable compromise. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
            storageAccount.CreateCloudTableClient().CreateTableIfNotExist(LogTableName);
        }

        /// <summary>
        /// Place the entry into the log.
        /// </summary>
        static public void Entry(int code, SEV_CODES sev, string msg, string loc) { Entry(code, sev, msg, loc, ""); }
        static public void Entry(int code, SEV_CODES sev, string msg, string loc, string who)
        {
            lock (HelperLock)
            {
                try
                {
                    if (MAX_SEV.Equals(-1) || MAX_SEV >= Convert.ToInt32(sev))
                    {
                        string fmtMsg = "";
                        if (code != 0) fmtMsg += code.ToString() + ": ";
                        if (loc.Length > 0) fmtMsg += " : (- " + loc + " -) ";
                        if (who.Length > 0) fmtMsg += " : (id-" + who + " -) ";
                        fmtMsg += msg;
                        LogLedger entry = new LogLedger(Convert.ToInt32(sev), fmtMsg);
                        /* Write to Database */
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                        TableServiceContext context = tableClient.GetDataServiceContext();
                        context.AddObject(LogTableName, entry);
                        context.SaveChangesWithRetries();
                        WSHelpers.ServiceLedger.Count(CNT_CODES.CNT_WT_LOG);
                    }
                }
                catch { }// Not much recourse if this fails. 
            }
        }

        /// <summary>
        /// Read all log records that fall within a given date. Returns an IEnumeration that can be used to get the actual
        /// data. Use a "foreach" to read the actual data, and it will automatically only pull down the chuncks it needs.
        /// Note: The DateTime Min and Max dates are not valid inputs and will cause an exception.
        /// </summary>
        static public CloudTableQuery<LogLedger> Read(DateTime start, DateTime end)
        {
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    CloudTableQuery<LogLedger> itemsHold = (from entry in context.CreateQuery<LogLedger>(LogTableName)
                                                            where (entry.Timestamp > start && entry.Timestamp < end)
                                                            select entry).AsTableServiceQuery<LogLedger>();
                    return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Read(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// Read all log records. Returns an IEnumeration that can be used to get the actual data. Use a "foreach" to read the
        /// actual data, and it will automatically only pull down the chuncks it needs.
        /// </summary>
        static public CloudTableQuery<LogLedger> Read()
        {
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    CloudTableQuery<LogLedger> itemsHold = (from entry in context.CreateQuery<LogLedger>(LogTableName)
                                                            where (entry.PartitionKey.Equals(ServiceLedger.ApplicationName))
                                                            select entry).AsTableServiceQuery<LogLedger>();
                    return itemsHold;
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Read", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove a Log Record.
        /// </summary>
        static public void Delete(string key)
        {
            lock (HelperLock)
            {
                try
                {
                    LogLedger entry = new LogLedger(key);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    context.AttachTo(LogTableName, entry, "*"); // Ignore etag on db, which will not match default one.
                    context.DeleteObject(entry);
                    context.SaveChangesWithRetries();
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Delete", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove any Log Records in the date range.
        /// </summary>
        static public void Delete(DateTime start, DateTime end)
        {
            CloudTableQuery<LogLedger> backing = ServiceLedger.Read(start, end);
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    foreach (LogLedger entry in backing)
                    {
                        context.Detach(entry);
                        context.AttachTo(LogTableName, entry, "*"); // Ignore etag on db, so it always get deleted.
                        context.DeleteObject(entry);
                    }
                    context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.Batch);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Delete(ByDate)", realMsg);
                }
            }
        }

        /// <summary>
        /// Remove all Log Records.
        /// </summary>
        static public void Delete()
        {
            CloudTableQuery<LogLedger> backing = ServiceLedger.Read();
            lock (HelperLock)
            {
                try
                {
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    TableServiceContext context = tableClient.GetDataServiceContext();
                    foreach (LogLedger entry in backing)
                    {
                        context.Detach(entry);
                        context.AttachTo(LogTableName, entry, "*"); // Ignore etag on db, so it always get deleted.
                        context.DeleteObject(entry);
                    }
                    context.SaveChangesWithRetries(System.Data.Services.Client.SaveChangesOptions.Batch);
                }
                catch (Exception ex)
                {   // The Storage Exceptions all bury their message in the inner exception, so we need to dig it out.
                    string realMsg = parseTSErr(ex.InnerException.Message);
                    throw new ClassExp(ClassExp.EXP_CODES.EXP_TS_FAIL, "ServiceLedger.Delete", realMsg);
                }
            }
        }
        static public string ApplicationName { get { return m_AppName; } }

        static public string LogTableName { get { return m_LogTableName; } }

        static public string CONNECT_SET_NAME { get { return m_connectSettingName; } }

        /// <summary>
        /// Place a record in the Audit log.
        /// It is a little hooky, but the sql instance can kill objects in our connection pool without
        /// us realizing it.  In that case, the recommended action is to retry, which we do once here.
        /// </summary>
        static public void Audit(ClassAud msg) { Audit(msg, false); }
        static public void Audit(ClassAud msg, bool encypt)
        {
            lock (HelperLock)
            {
                string holdParm = encypt ? "" : msg.Parameters;
                byte[] holdParmRaw = encypt ? msg.ParametersRaw : new byte[0];
                try
                {
                    DbSqlServer db = new DbSqlServer();
                    SqlParameter[] audParms = {
                    new SqlParameter("@UserTrack", msg.UserId),
                    new SqlParameter("@Machine", msg.Machine),
                    new SqlParameter("@ApplicationName", msg.AppName),
                    new SqlParameter("@MethodName", msg.MethodName),
                    new SqlParameter("@Parameters", holdParm),
                    new SqlParameter("@ParametersRaw", holdParmRaw),
                    new SqlParameter("@Source", msg.Source),
                    new SqlParameter("@Billing", msg.Billing),
                    new SqlParameter("@Signature", msg.Signature)
                    };
                    db.ExecuteNonQuery("NewAuditRec", audParms);
                }
                catch
                {   // Second Try
                    try
                    {
                        Entry((int)ClassExp.EXP_CODES.EXP_NODATA, SEV_CODES.SEV_INFO, "The Audit failed to complete, retrying... " + msg.MethodName, "Audit");
                        DbSqlServer db = new DbSqlServer();
                        SqlParameter[] audParms = {
                        new SqlParameter("@UserTrack", msg.UserId),
                        new SqlParameter("@Machine", msg.Machine),
                        new SqlParameter("@ApplicationName", msg.AppName),
                        new SqlParameter("@MethodName", msg.MethodName),
                        new SqlParameter("@Parameters", holdParm),
                        new SqlParameter("@ParametersRaw", holdParmRaw),
                        new SqlParameter("@Source", msg.Source),
                        new SqlParameter("@Billing", msg.Billing),
                        new SqlParameter("@Signature", msg.Signature)
                        };
                        db.ExecuteNonQuery("NewAuditRec", audParms);
                        Entry(18000, SEV_CODES.SEV_INFO, "Audit worked on 2nd try. Method name: " + msg.MethodName, "Audit");
                    }
                    catch (Exception ex)
                    {
                        Entry((int)ClassExp.EXP_CODES.EXP_NODATA, SEV_CODES.SEV_INFO, "Audit failed on 2nd try. Method name: " + msg.MethodName + "Error: " + ex.Message, "Audit");
                        return; // We do not want Audit writes to affect regular operations.
                    }
                }
            }
        }

        /// <summary>
        /// Increment a record in the Count table.
        /// The Count table works by writing a record with a time stamp as an increment.  Then one can 
        /// just do a sql count with a date range on the counter type to find the total.
        /// It is a little hooky, but the sql instance can kill objects in our connection pool without
        /// us realizing it.  In that case, the recommended action is to retry, which we do once here.
        /// </summary>
        /// 
        public enum CNT_CODES
        {
            CNT_WUAPI_CALLS = 1,    // Weather Underground API tries
            CNT_WUAPI_WORKS = 2,    // Weather Underground API success
            CNT_CACHE_QNF = 3,      // Query Not Found cache hit
            CNT_WUAPI_QNF = 4,      // Query Not Found rejection on API call
            CNT_WT_LOG = 5,         // An entry was written to the log
            CNT_WT_SEARCH = 6,      // Each time a Search is called 
            CNT_WT_WSBASIC = 7,     // Weather Station Basic 
            CNT_WT_WSFORCE = 8,     // Weather Station Forced
            CNT_WT_WSSMALL = 9,     // Weather Station Small/Slim
            CNT_WT_WSDETAIL = 10,   // Weather Station Details
            CNT_WT_PREPAQ = 11,     // Get a PrePaq
            CNT_WT_SIGNUP = 12,     // Registration
            CNT_WT_QUEUE = 13,      // Processed out of weather queue
            CNT_CACHE_LOAD = 14,    // Count of stations loaded into the cache from storage.
            CNT_RETURN_NULL = 15,   // Count of station requests returned to client as empty response.
            CNT_CACHE_BACKFAIL = 16 // Count of stations failed to save into the table storage as backing cache.

        };
        static public void Count(CNT_CODES type)
        {
            lock (HelperLock)
            {
                try
                {
                    DbSqlServer db = new DbSqlServer();
                    SqlParameter[] cntParms = {
                    new SqlParameter("@Counter", Convert.ToInt32(type))
                    };
                    db.ExecuteNonQuery("NewCountRec", cntParms);
                }
                catch
                {   // Second Try
                    try
                    {
                        Entry((int)ClassExp.EXP_CODES.EXP_NODATA, SEV_CODES.SEV_INFO, "The Count failed to complete, retrying... " + type.ToString(), "Count");
                        DbSqlServer db = new DbSqlServer();
                        SqlParameter[] cntParms = {
                        new SqlParameter("@Counter", Convert.ToInt32(type))
                        };
                        db.ExecuteNonQuery("NewCountRec", cntParms);
                        Entry(18000, SEV_CODES.SEV_INFO, "Count worked on 2nd try. Method name: " + type.ToString(), "Count");
                    }
                    catch (Exception ex)
                    {
                        Entry((int)ClassExp.EXP_CODES.EXP_NODATA, SEV_CODES.SEV_INFO, "Count failed on 2nd try. Method name: " + type.ToString() + "Error: " + ex.Message, "Count");
                        return; // We do not want Count increments to affect regular operations.
                    }
                }
            }
        }

        // **********************************************************************************
        // The Tag routines will create a tag based on the name and covert it all to a string.
        // **********************************************************************************
        static public string Tag(string name, int[] value)
        {
            string holdTag = "";
            if (value != null)
            {
                for (int i = 0; i < value.Length; ++i) holdTag += Tag(i.ToString(), value[i]);
            }
            return Tag(name, holdTag);
        }

        static public string Tag(string name, string[] value)
        {
            string holdTag = "";
            if (value != null)
            {
                for (int i = 0; i < value.Length; ++i) holdTag += Tag(i.ToString(), value[i]);
            }
            return Tag(name, holdTag);
        }

        static public string Tag(string name, bool value)
        {
            string holdTag = value.ToString();
            return Tag(name, holdTag);
        }

        static public string Tag(string name, DateTime value)
        {
            string holdTag = "";
            if (value != null)
                holdTag = value.ToString();
            return Tag(name, holdTag);
        }

        static public string Tag(string name, int value)
        {
            string holdTag = value.ToString();
            return Tag(name, holdTag);
        }

        static public string Tag(string name, float value)
        {
            string holdTag = value.ToString();
            return Tag(name, holdTag);
        }

        static public string Tag(string name, double value)
        {
            string holdTag = value.ToString();
            return Tag(name, holdTag);
        }

        static public string Tag(string name, string value)
        {
            if (name == null) return "";
            string valStr = "";
            if (value != null) valStr = value;
            return "<" + name + ">" + valStr + "</" + name + ">";
        }

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
