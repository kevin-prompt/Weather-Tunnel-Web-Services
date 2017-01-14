using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.IO;
using System.Configuration;

namespace ViewLog
{
    public partial class Main : Form
    {
        private XmlDocument details = null;
        private const string TS_RANGE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz"; // Dates expected to be in ISO 8601 format: yyyy-MM-ddTHH:mm:ss.fffffffzzz (where zzz = +00:00)
        private const string EOL = "\u000D\u000A";

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            tsStart.Value = DateTime.Now.AddDays(-7);
            tsEnd.Value = DateTime.Now;
        }

        #region Buttons

        private void btnSource_Click(object sender, EventArgs e)
        {
            fileSource.InitialDirectory = Application.StartupPath;
            fileSource.Filter = "Log Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (fileSource.ShowDialog().Equals(DialogResult.OK))
            {
                EnterTextBox(txtSource, "Source File");
                txtSource.Text = fileSource.FileName;
                rdoLocalFile.Checked = true;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Text = "";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            fileDestination.InitialDirectory = Application.StartupPath;
            fileDestination.DefaultExt = "xml";
            fileDestination.FileName = "ViewLog.xml";
            if (fileDestination.ShowDialog().Equals(DialogResult.OK))
            {
                details.Save(fileDestination.OpenFile());
                fileDestination.Dispose();
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            IList<LogRecord> data = GetAllInner(LogRecord.LABLE_REC, details);
            int recCnt = 1;
            txtLog.Text = "Starting the Deletion process for " + data.Count.ToString() + " records" + EOL;
            foreach (LogRecord rec in data)
            {
                string path = LoadFromConfiguration("appSettings", "deleteUtility", "");
                if (!path.LastIndexOf("/").Equals(path.Length - 1)) path += "/";
                path += rec.LedgerID;
                IList<string> parms = new List<string>();
                parms.Add("ticket=" + LoadFromConfiguration("appSettings", "supportKey", ""));
                string webSource = getREST(path, parms);
                txtLog.Text += "Record " + recCnt.ToString() + " deleted... ID= " + rec.LedgerID + EOL;
                recCnt++;
            }
            txtLog.Text += "Finished the Deletion process.";    
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (rdoLocalFile.Checked)
            {
                details = new XmlDocument();
                details.Load(txtSource.Text);
                FeedMaster(txtLog, GetAllInner(LogRecord.LABLE_REC, details));
            }
            else   // Web Service
            {
                details = new XmlDocument();
                string path = LoadFromConfiguration("appSettings", "searchUtility", "");
                IList<string> parms = new List<string>();
                parms.Add("ticket=" + LoadFromConfiguration("appSettings", "supportKey", ""));
                parms.Add("start=" + tsStart.Value.ToString(TS_RANGE));
                parms.Add("end=" + tsEnd.Value.ToString(TS_RANGE));
                string webSource = getREST(path, parms);
                details.LoadXml(webSource);
                FeedMaster(txtLog, GetAllInner(LogRecord.LABLE_REC, details));
            }
        }

        private void btnDateAgain_Click(object sender, EventArgs e)
        {
            if(details != null)
                FeedMaster(txtLog, GetAllInner(LogRecord.LABLE_REC, details));
        }


        #endregion

        #region Text Entry
        private void EnterTextBox(Control me, string defaultText)
        {
            if (me.Text.Equals(defaultText))
            {
                me.Text = "";
                me.Font = TextBox.DefaultFont;
                me.ForeColor = Color.Black;
            }
            else
            {   // Highlight the text if it is not default.
                System.Windows.Forms.TextBox castTextbox = (System.Windows.Forms.TextBox)me;
                castTextbox.SelectAll();
            }
        }
        private void LeaveTextBox(Control me, string defaultText)
        {
            if (me.Text.Equals(""))
            {
                me.Text = defaultText;
                me.Font = new Font(TextBox.DefaultFont, me.Font.Style | FontStyle.Bold | FontStyle.Italic); ;
                me.ForeColor = Color.Silver;
            }
        }

        private void txtSource_Enter(object sender, EventArgs e)
        {
            EnterTextBox(ActiveControl, "Source File");
        }

        private void txtSource_Leave(object sender, EventArgs e)
        {
            LeaveTextBox(txtSource, "Source File");
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            FeedMaster(txtLog, GetAllInner(LogRecord.LABLE_REC, details));    
        }
        #endregion


        private void FeedMaster(TextBox log, IList<LogRecord> master)
        {
            log.Text = "";
            StringBuilder holdrec = new StringBuilder(master.Count * 80);
            foreach (LogRecord rec in master)
            {
                holdrec.Append("TS-: " + rec.LedgerTimestamp.ToString("MMM-dd-yyyy @ hh:mm:ss t"));
                holdrec.Append("  -LVL-:(" + rec.LedgerSeverity.ToString() + ")");
                holdrec.Append("  -MSG-: " + rec.LedgerMessage.Replace("\n"," "));
                holdrec.Append("\u000D\u000A");
            }
            log.Text = holdrec.ToString();
            lblRawCnt.Text = "Count = " + master.Count.ToString();
        }

        private IList<LogRecord> GetAllInner(string tag, XmlDocument doc)
        {
            IList<LogRecord> data = new List<LogRecord>();
            if (doc == null) return data;
            XmlElement root = doc.DocumentElement;
            XmlNodeList xNodes = root.GetElementsByTagName(tag);

            foreach (XmlNode record in xNodes)
            {
                LogRecord holdRec = new LogRecord();
                XmlNodeList parameters = record.ChildNodes;
                foreach (XmlNode item in parameters)
                {
                    string holdName = item.Name;
                    if(holdName.Equals(LogRecord.LABLE_ID))
                    {
                        holdRec.LedgerID = item.InnerText;
                    }
                    if (holdName.Equals(LogRecord.LABLE_APP))
                    {
                        holdRec.LedgerApplication = item.InnerText;
                    }
                    if (holdName.Equals(LogRecord.LABLE_LVL))
                    {
                        holdRec.LedgerSeverityConvertString(item.InnerText);
                    }
                    if (holdName.Equals(LogRecord.LABLE_MSG))
                    {
                        holdRec.LedgerMessage = item.InnerText;
                    }
                    if (holdName.Equals(LogRecord.LABLE_TS))
                    {
                        holdRec.LegerTimestampConvertString(item.InnerText);
                    }
                }
                if(Filter(holdRec))
                    data.Add(holdRec);
            }

            return data;
        }

        /// <summary>
        /// The filter will check if the log record matches the desired criteria.  Any failure
        /// will cut it out of the display.  To support that, each filter requries that the 
        /// data has not already be thrown out.  That is, one it is not "approved" no point in
        /// checking anymore.
        /// </summary>
        private bool Filter(LogRecord holdRec)
        {
            bool approved = true;
            
            // Date Filter
            if (approved && !(tsStart.Value < DateTime.Now.AddYears(-10)))
            {
                approved = !(holdRec.LedgerTimestamp < tsStart.Value); // If earlier than start filter, exclude
            }
            if (approved && !(tsEnd.Value > DateTime.Now.AddYears(1)))
            {
                approved = !(holdRec.LedgerTimestamp > tsEnd.Value);    // If later than end filter, exclude
            }

            // Message Filter
            if (approved && !txtSearch.Size.Equals(0))
            {
                approved = holdRec.LedgerMessage.IndexOf(txtSearch.Text) >= 0; // If text not in msg, exclude
            }


            return approved;
        }

        #region Utilities
        /// <summary>
        /// This will create the URI and properly format the parameters.  Any headers can be added
        /// before the Http command is called.
        /// </summary>
        private string getREST(string path, IList<string> parameters)
        {
            string endpoint = LoadFromConfiguration("appSettings", "endpointREST", "");
            if (!endpoint.LastIndexOf("/").Equals(endpoint.Length - 1)) endpoint += "/";
            endpoint += path;

            string query = "?";
            foreach (string parm in parameters)
            {
                query += parm + "&";
            }
            if ((query.LastIndexOf("&").Equals(query.Length - 1))) query = query.Substring(0, query.Length - 1);


            // This did not work on first go, but not really needed here, so will just skip it for now.
            IList<string> headers = new List<string>();
            //headers.Add("user-agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            return HttpGet(endpoint, query, headers);
        }

        /// <summary>
        /// This will combine the URI + parameters, add any headers and perform a GET.
        /// </summary>
        public string HttpGet(string URI, string parameters, IList<string> headers)
        {
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(URI+parameters);
                req.Method = "GET";

                // This did not work on first go, but not really needed here, so will just skip it for now.
                //foreach (WebHeaderCollection header in req.Headers)
                //{
                //    req.Headers.Add(header);
                //}

                System.Net.WebResponse resp = req.GetResponse();
                if (resp == null) return null;
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                return sr.ReadToEnd().Trim();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return ""; }
        }

        /// <summary>
        /// One way to read any type of configuration data.
        /// </summary>
        /// <param name="key">For example: appSettings</param>
        /// <param name="name">For example: ApplicationName</param>
        /// <param name="defaultValue">For example: Unknown</param>
        public string LoadFromConfiguration(string key, string name, string defaultValue)
        {
            System.Configuration.Configuration config;
            string holdRtn = defaultValue;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection section = (AppSettingsSection)config.GetSection(key);
                if (section != null)
                {
                    KeyValueConfigurationElement setting = section.Settings[name];
                    if (setting != null)
                        holdRtn = setting.Value;
                }
            }
            catch
            { }
            return holdRtn;
        }

        /// <summary>
        /// One way to save any type of configuration data
        /// </summary>
        /// <param name="key">For example: appSettings</param>
        /// <param name="name">For example: ApplicationName</param>
        /// <param name="value">For example: WebServiceViewLog</param>
        public void SaveToConfiguration(string key, string name, string value)
        {
            try
            {
                System.Configuration.Configuration config;
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection section = (AppSettingsSection)config.GetSection(key);
                if (section != null)
                {
                    section.Settings[name].Value = value;
                    config.Save();
                }
            }
            catch
            { }
        }

        #endregion
    }

}
