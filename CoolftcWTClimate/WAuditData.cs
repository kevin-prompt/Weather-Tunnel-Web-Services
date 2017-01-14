using System;
using WSHelpers;
using System.Data.SqlClient;
using System.Data;

namespace Coolftc.WTClimate
{
    public class AuditDisplay
    {
        // The AuditDispaly class is used to specify a set of criteria to be used when requesting audit records.
        int m_sortCode;
        bool m_orderASC;
        string m_userID;
        string m_machine;
        string m_application;
        string m_method;
        int m_billing;
        DateTime m_start;
        DateTime m_end;
        bool m_decrypt;
        const string EMPTY = "";

        public string TAG()
        {
            string holdTag = "";
            holdTag += ServiceLedger.Tag("sortCode", sortCode);
            holdTag += ServiceLedger.Tag("orderASC", orderASC);
            holdTag += ServiceLedger.Tag("userID", userID);
            holdTag += ServiceLedger.Tag("machine", machine);
            holdTag += ServiceLedger.Tag("application", application);
            holdTag += ServiceLedger.Tag("method", method);
            holdTag += ServiceLedger.Tag("billing", billing);
            holdTag += ServiceLedger.Tag("start", start);
            holdTag += ServiceLedger.Tag("end", end);
            holdTag += ServiceLedger.Tag("decrypt", decrypt);
            return holdTag;
        }

        public int sortCode
        {
            get { return m_sortCode; }
            set { m_sortCode = value; }
        }
        public bool orderASC
        {
            get { return m_orderASC; }
            set { m_orderASC = value; }
        }
        public string userID
        {
            get { return m_userID; }
            set { m_userID = value ?? EMPTY; }
        }
        public string machine
        {
            get { return m_machine; }
            set { m_machine = value ?? EMPTY; }
        }
        public string application
        {
            get { return m_application; }
            set { m_application = value ?? EMPTY; }
        }
        public string method
        {
            get { return m_method; }
            set { m_method = value ?? EMPTY; }
        }
        public int billing
        {
            get { return m_billing; }
            set { m_billing = value; }
        }
        public DateTime start
        {
            get { return m_start; }
            set { m_start = value; }
        }
        public DateTime end
        {
            get { return m_end; }
            set { m_end = value; }
        }
        public bool decrypt
        {
            get { return m_decrypt; }
            set { m_decrypt = value; }
        }
    }

    public class WCountDataHelper
    {
        /// <summary>
        /// Return the count value for a specific Count Type over a (optionally) specified data range.
        /// </summary>
        static public long Total(ServiceLedger.CNT_CODES type) { return Total(type, DateTimeOffset.MinValue, DateTimeOffset.MaxValue); }
        static public long Total(ServiceLedger.CNT_CODES type, DateTimeOffset start, DateTimeOffset end)
        {
            // Adjust dates to reasonable values
            DateTimeOffset sqlMinDate = new DateTimeOffset(1753, 1, 1, 0, 0, 1, TimeSpan.Zero);
            DateTimeOffset sqlMaxDate = new DateTimeOffset(9999, 12, 31, 0, 0, 1, TimeSpan.Zero);
            if (start < sqlMinDate) start = sqlMinDate;
            if (end < sqlMinDate) end = sqlMaxDate;
            if (start > sqlMaxDate) start = sqlMinDate;
            if (end > sqlMaxDate) end = sqlMaxDate;
            if (start > end) start = end;

            DbSqlServer db = new DbSqlServer();
            SqlParameter[] countParm = { 
            new SqlParameter("@Counter", Convert.ToInt32(type)),
            new SqlParameter("@DateFirst", start),
            new SqlParameter("@DateLast", end)
            };
            return Convert.ToInt32(db.ExecuteScalar("GetCountByDate", countParm));

        }
    }

    public class WAuditDataHelper
    {
        /// <summary>
        ///  _+_+_Get a collection of Audit keys based on the input criteria.
        /// </summary>
        static public int[] GetAuditRange(AuditDisplay criteria, int first, int request, out int total)
        {
            // Adjust dates to reasonable values
            DateTime sqlMinDate = new DateTime(1753, 1, 1);
            DateTime sqlMaxDate = new DateTime(9999, 12, 31);
            if (criteria.start < sqlMinDate) criteria.start = sqlMinDate;
            if (criteria.end < sqlMinDate) criteria.end = sqlMaxDate;
            if (criteria.start > sqlMaxDate) criteria.start = sqlMinDate;
            if (criteria.end > sqlMaxDate) criteria.end = sqlMaxDate;
            if (criteria.start > criteria.end) criteria.start = criteria.end;

            // Generate the where and sort clauses to be used by the SQL.
            string clauseWhere = "";
            clauseWhere = "CreateDate >= '" + criteria.start.ToString() + "' and CreateDate <= '" + criteria.end.ToString() + "' ";
            if (criteria.userID.Length > 0) clauseWhere += "and UserTrack = '" + criteria.userID + "' ";
            if (criteria.machine.Length > 0) clauseWhere += "and Machine = '" + criteria.machine + "' ";
            if (criteria.application.Length > 0) clauseWhere += "and ApplicationName = '" + criteria.application + "' ";
            if (criteria.method.Length > 0) clauseWhere += "and MethodName = '" + criteria.method + "' ";
            if (criteria.billing >= 0) clauseWhere += "and Billing = " + criteria.billing.ToString() + " ";
            string clauseOrderBy = "CreateDate";
            if (criteria.sortCode.Equals(1)) clauseOrderBy = "UserTrack";
            string clauseDirection = "DESC";
            if (criteria.orderASC) clauseDirection = "ASC";

            // Execute the SQL to get the desired set of Audit Keys
            // Note: the database is One(1) indexed, while the arrays are Zero(0) indexed, and the user might be either,
            // so we have to dance around that somewhat.
            DbSqlServer db = new DbSqlServer();
            SqlParameter[] auditParm = { 
                new SqlParameter("@WhereClause", clauseWhere),
                new SqlParameter("@SortBy", clauseOrderBy),
                new SqlParameter("@SortOrder", clauseDirection)
            };
            DataSet dsAudits = db.ExecuteDataset("GetAuditRange", auditParm);
            total = dsAudits.Tables[0].Rows.Count;
            if (first.Equals(0)) first++;           // Some people might think this is zero indexed
            if (request.Equals(0)) request = total; // Why call if you don't want anything back?
            int rtnKeys = System.Math.Min(request, total - first) + 1;
            if (rtnKeys < 0) rtnKeys = 0;
            int[] auditKeys = new int[rtnKeys];

            for (int cnt = 0, pos = first - 1; cnt < rtnKeys; ++cnt, ++pos)
            {
                DataRow row = dsAudits.Tables[0].Rows[pos];
                auditKeys[cnt] = row["AuditID"].GetType().Name == "DBNull" ? 0 : Convert.ToInt32(row["AuditID"]);
            }

            return auditKeys;
        }

        /// <summary>
        /// _+_+_Returns the requested set of Audit records.
        /// </summary>
        static public SystemAudit[] AuditEntrys(int[] localKeys, bool decrypt)
        {
            SystemAudit[] recs = new SystemAudit[localKeys.Length];
            for (int cnt = 0; cnt < localKeys.Length; ++cnt)
            {
                SystemAudit oneAudit = AuditEntry(localKeys[cnt], decrypt);
                recs[cnt] = oneAudit;
            }
            return recs;
        }

        /// <summary>
        /// _+_+_Get a single audit record.
        /// </summary>
        static public SystemAudit AuditEntry(int id, bool decrypt)
        {
            DbSqlServer db = new DbSqlServer();

            SqlParameter[] parameter = { new SqlParameter("@auditID", id) };
            DataRow rowAudit = db.ExecuteDataRow("GetAuditInfo", parameter);
            if (rowAudit == null)
                throw new ClassExp(ClassExp.EXP_CODES.EXP_NODATA, "Static Method SecurityHelper.AuditEntry", "No Audit Rec. Found for id:" + id.ToString());

            // Fill in the data
            SystemAudit entry = new SystemAudit();
            entry.auditID = id;
            entry.userID = rowAudit["UserTrack"].GetType().Name == "DBNull" ? "" : rowAudit["UserTrack"].ToString();
            entry.machine = rowAudit["Machine"].GetType().Name == "DBNull" ? "" : rowAudit["Machine"].ToString();
            entry.application = rowAudit["ApplicationName"].GetType().Name == "DBNull" ? "" : rowAudit["ApplicationName"].ToString();
            entry.method = rowAudit["MethodName"].GetType().Name == "DBNull" ? "" : rowAudit["MethodName"].ToString();
            entry.parameters = rowAudit["Parameters"].GetType().Name == "DBNull" ? "" : rowAudit["Parameters"].ToString();
            byte[] parametersRaw = rowAudit["ParametersRaw"].GetType().Name == "DBNull" ? new byte[0] : (byte[])rowAudit["ParametersRaw"];
            entry.source = rowAudit["Source"].GetType().Name == "DBNull" ? "" : rowAudit["Source"].ToString();
            entry.billing = rowAudit["Billing"].GetType().Name == "DBNull" ? 0 : Convert.ToInt32(rowAudit["Billing"]);
            entry.timestamp = rowAudit["CreateDate"].GetType().Name == "DBNull" ? DateTime.MinValue : (DateTime)rowAudit["CreateDate"];
            byte[] holdSig = rowAudit["Signature"].GetType().Name == "DBNull" ? new byte[0] : (byte[])rowAudit["Signature"];
            entry.valid = -1;
            if (holdSig.Length > 0)
            {
                ClassAud auRec = new ClassAud(entry.userID, entry.machine, entry.source, entry.application, entry.method, entry.parameters, entry.billing);
                if (decrypt && parametersRaw.Length > 0)
                {
                    auRec.ParametersRaw = parametersRaw;
                    entry.parameters = auRec.GetParametersRaw(parametersRaw);
                }
                entry.valid = auRec.Validate(holdSig) ? 1 : 0;

            }

            return entry;
        }

        /// <summary>
        /// _+_+_Returns the requested set of Audit records.
        /// </summary>
        static public int AuditDeletes(int[] localKeys)
        {
            DbSqlServer db = new DbSqlServer();
            for (int cnt = 0; cnt < localKeys.Length; ++cnt)
            {
                SqlParameter[] parameter = { new SqlParameter("@auditID", localKeys[cnt]) };
                db.ExecuteNonQuery("DelAuditInfo", parameter);
            }
            return localKeys.Length;
        }

    }
}