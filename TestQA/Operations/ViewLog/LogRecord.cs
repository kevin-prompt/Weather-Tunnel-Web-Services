using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewLog
{
    class LogRecord
    {
        public const string LABLE_REC = "LedgerLog";
        public const string LABLE_APP = "ledgerApplication";
        public const string LABLE_ID = "ledgerID";
        public const string LABLE_MSG = "message";
        public const string LABLE_LVL = "severity";
        public const string LABLE_TS = "timestamp";

        private string mLedgerApplication;
        private string mLedgerID;
        private string mLedgerMessage;
        private int mLedgerSeverity;
        private DateTime mLedgerTimestamp;


        public LogRecord()
        {
            mLedgerApplication = "Unknown";
            mLedgerID = "";
            mLedgerMessage = "N/A";
            mLedgerSeverity = -1;
            mLedgerTimestamp = DateTime.MinValue;
        }

        #region Properties

        public string LedgerApplication
        {
            get { return mLedgerApplication; }
            set { mLedgerApplication = value; }
        }

        public string LedgerID
        {
            get { return mLedgerID; }
            set { mLedgerID = value; }
        }

        public string LedgerMessage
        {
            get { return mLedgerMessage; }
            set { mLedgerMessage = value; }
        }

        public int LedgerSeverity
        {
            get { return mLedgerSeverity; }
            set { mLedgerSeverity = value; }
        }
        public void LedgerSeverityConvertString(string value)
        {
            try { int.TryParse(value, out mLedgerSeverity); } catch { mLedgerSeverity = -1; }
        }

        public DateTime LedgerTimestamp
        {
            get { return mLedgerTimestamp; }
            set { mLedgerTimestamp = value; }
        }
        public void LegerTimestampConvertString(string value)
        {
            try { DateTime.TryParse(value, out mLedgerTimestamp); } catch { mLedgerTimestamp = DateTime.MinValue; }
        }

        #endregion
    }
}
