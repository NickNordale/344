using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using PA3ClassLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);

        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable urlTable = tableClient.GetTableReference("crawledURLs");

        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue startStopQueue = queueClient.GetQueueReference("startstop");

        private Boolean initialized = false;

        [WebMethod]
        public string startCrawling()
        {
            if (!initialized)
            {
                startStopQueue.CreateIfNotExists();
                urlTable.CreateIfNotExists();
                urlQueue.CreateIfNotExists();

                CloudQueueMessage cnnRobots = new CloudQueueMessage("http://www.cnn.com/robots.txt");
                urlQueue.AddMessage(cnnRobots);

                CloudQueueMessage brRobots = new CloudQueueMessage("http://www.bleacherreport.com/robots.txt");
                urlQueue.AddMessage(brRobots);

                CloudQueueMessage startMsg = new CloudQueueMessage("start init");
                startStopQueue.AddMessage(startMsg);

                initialized = true;
            }
            else
            {
                CloudQueueMessage startMsg = new CloudQueueMessage("start");
                startStopQueue.AddMessage(startMsg);
            }

            return "Started crawling.";
        }

        [WebMethod]
        public string stopCrawling()
        {
            startStopQueue.CreateIfNotExists();
            CloudQueueMessage stopMsg = new CloudQueueMessage("stop");
            startStopQueue.AddMessage(stopMsg);

            return "Stopped crawling.";
        }
    }
}
