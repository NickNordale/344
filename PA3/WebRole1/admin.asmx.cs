using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
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
        private static CloudQueue startStopQueue = queueClient.GetQueueReference("startStop");

        [WebMethod]
        public string startCrawling()
        {
            /*urlTable.DeleteIfExistsAsync();
            urlQueue.DeleteIfExistsAsync();
            startStopQueue.DeleteIfExistsAsync();*/

            urlTable.CreateIfNotExists();
            urlQueue.CreateIfNotExists();
            startStopQueue.CreateIfNotExists();

            string cnnRobots = "http://www.cnn.com/robots.txt";
            string brRobots = "http://bleacherreport.com/robots.txt";

            CloudQueueMessage cnn = new CloudQueueMessage(cnnRobots);
            urlQueue.AddMessage(cnn);

            CloudQueueMessage br = new CloudQueueMessage(brRobots);
            urlQueue.AddMessage(br);

            CloudQueueMessage initiateCrawl = new CloudQueueMessage("start");
            startStopQueue.AddMessage(initiateCrawl);

            WebClient wc = new WebClient();
            string sout = wc.DownloadString("http://www.cnn.com/robots.txt");

            return sout;
        }

        [WebMethod]
        public string pauseCrawling()
        {
            return "Paused crawling.";
        }

        [WebMethod]
        public string resumeCrawling()
        {
            return "Resumed crawling.";
        }
    }
}
