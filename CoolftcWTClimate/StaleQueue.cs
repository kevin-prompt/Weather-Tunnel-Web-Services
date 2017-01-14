using System;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Coolftc.WTClimate
{
    static public class StaleQueueHelper
    {
        static private CloudQueueClient clientCQ;
        static private string m_connectSettingName = "StaleQueueConnection";
        static private string m_queueName = "stalemessagequeue";

        static StaleQueueHelper()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(CONNECT_SET_NAME));
            clientCQ = storageAccount.CreateCloudQueueClient();
            CloudQueue msqueue = clientCQ.GetQueueReference(QueueName);
            msqueue.CreateIfNotExist();
        }

        static public CloudQueue StaleQueue { get { return clientCQ.GetQueueReference(QueueName); } }
        static public CloudQueueClient StaleClientCQ { get { return clientCQ; } }
        static public string CONNECT_SET_NAME { get { return m_connectSettingName; } }
        static public string QueueName { get { return m_queueName; } }


    }
}