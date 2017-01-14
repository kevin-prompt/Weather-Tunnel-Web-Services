using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Xml;

namespace Coolftc.WTClimate
{
    /// <summary>
    /// The Support API provides an interface to perform maintenance and get metadata for the Application. The methods 
    /// supported in this interface are not used by the Weather Tunnel application, but can affect the data returned by the Climate Interface.
    /// </summary>
    [ServiceContract(Name = "Support", Namespace = "http://coolftc.org/CoolftcWTSupport")]
    public interface ISupport
    {
        /// <summary>
        /// Add a destination to a prepaq.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/location/prepaq/new/{prepaq}/{name}?where={where}&ticket={ticket}")]
        void NewDestination(string ticket, string prepaq, string name, string where);

        /// <summary>
        /// Delete a destination from a prepaq.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/location/prepaq/del/{prepaq}/{name}?ticket={ticket}")]
        void DelDestination(string ticket, string prepaq, string name);

        /// <summary>
        /// Return the information for a specific registrations.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/register/{key}?ticket={ticket}")]
        string GetRegInfo(string ticket, string key);

        /// <summary>
        /// Change the activation on a registrations.  Usually to deactivate.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/register/chg/{key}?active={act}&ticket={ticket}")]
        void ChgRegActive(string ticket, string key, bool act);

        /// <summary>
        /// Returns a list of registration ids for the date range. Dates in ISO 8601 format.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/register/search?start={start}&end={end}&ticket={ticket}")]
        RegistrationID[] GetRegByDate(string ticket, string start, string end);

        /// <summary>
        /// Return a list of all log items from the ledger.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/log?ticket={ticket}")]
        LedgerLog[] GetLogList(string ticket);

        /// <summary>
        /// Return a list of specific log items from the ledger. Dates in ISO 8601 format.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/log/search?start={start}&end={end}&ticket={ticket}")]
        LedgerLog[] GetLogFilter(string ticket, string start, string end);

        /// <summary>
        /// Delete a specific log item.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/log/del?start={start}&end={end}&ticket={ticket}")]
        void DelLogFilter(string ticket, string start, string end);

        /// <summary>
        /// Delete a specific log item.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/log/del/{key}?ticket={ticket}")]
        void DelLogItem(string ticket, string key);

        /// <summary>
        /// Returns a range of audit data. The parameters are all optional for REST call.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/audit/search?first={first}&max={max}&sortcode={cSort}&orderasc={cASC}&userid={cUser}&machine={cMachine}&service={cAppl}&method={cMethod}&billing={cBill}&start={cStart}&end={cEnd}&ticket={ticket}")]
        SystemAudit[] GetAuditRange(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd);

        /// <summary>
        /// Returns a count of a range of audit data. The parameters are all optional for REST call.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/audit/search/count?first={first}&max={max}&sortcode={cSort}&orderasc={cASC}&userid={cUser}&machine={cMachine}&service={cAppl}&method={cMethod}&billing={cBill}&start={cStart}&end={cEnd}&ticket={ticket}")]
        int GetAuditCount(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd);

        /// <summary>
        /// Returns a count of a range of audit data. The parameters are all optional for REST call.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/audit/del/range?first={first}&max={max}&sortcode={cSort}&orderasc={cASC}&userid={cUser}&machine={cMachine}&service={cAppl}&method={cMethod}&billing={cBill}&start={cStart}&end={cEnd}&ticket={ticket}")]
        int DelAuditRange(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd);

        /// <summary>
        /// Delete log items found within a date range.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/stats?ticket={ticket}")]
        XmlElement GetStats(string ticket);

        /// <summary>
        /// Return the Version information of this web service.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/version")]
        string Version();
    }


    [DataContract]
    public class LedgerLog
    {
        // The LedgerLog class contains data about events that are specifically logged by the system.  Typically 
        // these events represent problems that have occured.  For example, any exceptions thrown will usually be
        // written to the ledger.
        string m_ledgerID;    // Unique ID for each log record.
        string m_ledgerApp;   // The application that wrote the log record.
        int m_severity;       // A severity code for the log item.  1=error, 2=important, 3=information, 4=debug
        string m_message;     // The freeform text of the log entry.  
        DateTime m_timestamp; // The date and time the log entry was made.

        [DataMember]
        public string ledgerID
        {
            get { return m_ledgerID; }
            set { m_ledgerID = value; }
        }
        [DataMember]
        public string ledgerApplication
        {
            get { return m_ledgerApp; }
            set { m_ledgerApp = value; }
        }
        [DataMember]
        public int severity
        {
            get { return m_severity; }
            set { m_severity = value; }
        }
        [DataMember]
        public string message
        {
            get { return m_message; }
            set { m_message = value; }
        }
        [DataMember]
        public DateTime timestamp
        {
            get { return m_timestamp; }
            set { m_timestamp = value; }
        }
    }

    [DataContract]
    public class SystemAudit
    {
        // The SystemAudit class contains data about the use of any web service method that has been executed.  It holds
        // information about the method, including any reasonable input parameters.  Some parameters are not logged by 
        // convention, passwords, images, etc.  Some parameters are encypted and require special authorization to view.
        int m_auditID;            // Unique ID for each log record.
        string m_userID;          // The email address (or some other public id) of the entity that triggered the audit entry.
        string m_machine;         // The name of the machine upon when the web service request was made.
        string m_application;     // The name of the specific web service used for this method call.
        string m_method;          // The specific method called by the user.
        string m_parameters;      // The input data used in the method call.
        string m_source;          // This is where the request came from, looking at IP address now.
        int m_billing;            // This is a “floating” data item.  It can be modified without invalidating the audit signature.
        int m_valid;              // When set to zero the digital signature failed, when set to 1 the digital signature worked.  
        DateTime m_timestamp;     // The date and time the log entry was made.

        [DataMember]
        public int auditID
        {
            get { return m_auditID; }
            set { m_auditID = value; }
        }
        [DataMember]
        public string userID
        {
            get { return m_userID; }
            set { m_userID = value; }
        }
        [DataMember]
        public string machine
        {
            get { return m_machine; }
            set { m_machine = value; }
        }
        [DataMember]
        public string application
        {
            get { return m_application; }
            set { m_application = value; }
        }
        [DataMember]
        public string method
        {
            get { return m_method; }
            set { m_method = value; }
        }
        [DataMember]
        public string parameters
        {
            get { return m_parameters; }
            set { m_parameters = value; }
        }
        [DataMember]
        public string source
        {
            get { return m_source; }
            set { m_source = value; }
        }
        [DataMember]
        public int billing
        {
            get { return m_billing; }
            set { m_billing = value; }
        }
        [DataMember]
        public int valid
        {
            get { return m_valid; }
            set { m_valid = value; }
        }
        [DataMember]
        public DateTime timestamp
        {
            get { return m_timestamp; }
            set { m_timestamp = value; }
        }
    }

    [DataContract]
    public class RegistrationID
    {
        // The RegistrationID class contains a registration id.
        string m_id;          // The generated id of the registration.
        bool m_active;        // If true active, if false deactivated.
        DateTime m_timestamp; // When created.
        string m_device;      // Unique data originally sent during registration.

        [DataMember]
        public string Id
        {
            get { return m_id; }
            set { m_id = value; }
        }

        [DataMember]
        public bool Active
        {
            get { return m_active; }
            set { m_active = value; }
        }

        [DataMember]
        public DateTime Timestamp
        {
            get { return m_timestamp; }
            set { m_timestamp = value; }
        }

        [DataMember]
        public string Device
        {
            get { return m_device; }
            set { m_device = value; }
        }
    }
}

