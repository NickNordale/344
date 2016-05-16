using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using PA3ClassLibrary;
//using Microsoft.ServiceBus;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);

        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable urlTable = tableClient.GetTableReference("crawledURLs");

        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue startStopQueue = queueClient.GetQueueReference("startstop");

        private HashSet<string> crawledLinks;
        private HashSet<string> disallowedLinks;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            Thread.Sleep(50);

            CloudQueueMessage statusMsg;

            //startStopQueue.CreateIfNotExists();

            if (startStopQueue.Exists() && (statusMsg = startStopQueue.GetMessage()) != null)
            {
                if (statusMsg.AsString == "start init")
                {
                    disallowedLinks = new HashSet<string>();

                    string cnnRobotsUrl = GetMessage();
                    List<string> cnnXml = ParseRobots(cnnRobotsUrl);

                    foreach (string cnnUrl in cnnXml)
                    {
                        CloudQueueMessage newMessage = new CloudQueueMessage(cnnUrl);
                        urlQueue.AddMessageAsync(newMessage);
                    }

                    string brRobotsUrl = GetMessage();
                    List<string> brXml = ParseRobots(brRobotsUrl);

                    foreach (string brUrl in brXml)
                    {
                        if (brUrl.Contains("/nba.xml"))
                        {
                            CloudQueueMessage newMessage = new CloudQueueMessage(brUrl);
                            urlQueue.AddMessageAsync(newMessage);
                        }                        
                    }
                }
                //else if (statusMsg.AsString == "start") { }
                //else { }
                startStopQueue.DeleteMessage(statusMsg);
            }

            

            

            //OnMessageOptions msgOptions = new OnMessageOptions();
            //startStopQueue.


            



            /* try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            } */
        }

        public string GetMessage()
        {
            CloudQueueMessage retrievedUrl = urlQueue.GetMessage();
            string urlString = retrievedUrl.AsString;

            urlQueue.DeleteMessage(retrievedUrl);

            return urlString;
        }

        public List<string> ParseRobots(string urlIn)
        {
            WebClient wc = new WebClient();
            Stream file = wc.OpenRead(urlIn);
            StreamReader sr = new StreamReader(file);
            List<string> lines = new List<string>();
            string currLine = "";

            while ((currLine = sr.ReadLine()) != null)
            {
                lines.Add(currLine);
            }

            List<string> xmlLinks = new List<string>();

            foreach (string line in lines)
            {
                if (line.StartsWith("Sitemap") && line.Trim().EndsWith(".xml") || line.Trim().EndsWith("nba.xml"))
                {
                    string smUrl = line.Substring(line.IndexOf("http"));
                    xmlLinks.Add(smUrl);
                }
                else if (line.StartsWith("Disallow"))
                {
                    string addDisallow = line.Trim().Substring(line.IndexOf('/'));
                    disallowedLinks.Add(addDisallow);
                }
            }

            // this makes either "cnn robots" or "bleacherreport robots"
            string title = urlIn.Substring(urlIn.IndexOf("www.") + 4);
            string titleFinal = title.Remove(title.IndexOf(".com")) + " robots";
            string encodedUrl = EncodeUrlInKey(urlIn);
            urlTableEntity insertUrl = new urlTableEntity(encodedUrl, titleFinal);
            TableOperation insertOperation = TableOperation.InsertOrReplace(insertUrl);
            urlTable.Execute(insertOperation);

            return xmlLinks;
        }

        /*public void crawl()
        {

        }*/

        private static String EncodeUrlInKey(String url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

        /*private static String DecodeUrlInKey(String encodedKey)
        {
            var base64 = encodedKey.Replace('_', '/');
            byte[] bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }*/

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
