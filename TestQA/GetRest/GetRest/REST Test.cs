using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Net;
using System.IO;
using System.Xml;

using WCFServiceWebRole1;

namespace GetRest
{
    public partial class Main : Form
    {
        private string baseXMLName = "GetRestRaw.xml";

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            wwwResponse.Location = new Point(0, 50);
            wwwResponse.Width = this.Width - 5;
            wwwResponse.Height = this.Height - 100;
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            wwwResponse.Width = this.Width - 5;
            wwwResponse.Height = this.Height - 100;
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            // Parse out the details 
            //XmlDocument details = new XmlDocument();
            //details.LoadXml(getREST(txtURI.Text));

            // Create Temp File for raw xml
            //details.Save(Application.StartupPath + baseXMLName);
            //wwwResponse.Url = new Uri("file://" + Application.StartupPath + baseXMLName);

            Service1Client svc = new Service1Client();
            MessageBox.Show(svc.GetHello());
            MessageBox.Show(svc.GetData(100));

        }

        /// <summary>
        /// This creates a uri by putting together a base url and the applicable path/parameters.
        /// It pretends to be a regular browser and performs a GET command, then just returns
        /// what ever comes back (xml in this case.)
        /// </summary>
        private string getREST(string path)
        {
            string endpoint = ConfigurationManager.AppSettings["endpointREST"].ToString();
            //if (!endpoint.LastIndexOf("/").Equals(endpoint.Length - 1)) endpoint += "/";
            Uri url = new Uri(endpoint + path);
            WebClient page = new WebClient();
            //page.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Stream data = page.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            string pageRaw = reader.ReadToEnd();
            data.Close();
            reader.Close();
            return pageRaw;
        }

    }
}
