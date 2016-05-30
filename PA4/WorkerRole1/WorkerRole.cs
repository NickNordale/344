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
using PA4ClassLibrary;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        // CPU
        private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        // RAM
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");

        private HashSet<string> disallowedLinks = new HashSet<string>();
        private List<string> xmlLinks = new List<string>();

        public static Queue<string> Last10 = new Queue<string>();
        public static List<string> Errors = new List<string>();

        private int urlsCrawled = 0;
        private int sitesindexed = 0;
        private int sizeOfIndex = 0;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                CrawlerParser webParser = new CrawlerParser();
                CloudQueueMessage statusMsg;
                while(true)
                {
                    if ((statusMsg = Storage.StatusQueue.PeekMessage()) != null)
                    {
                        if (statusMsg.AsString == "load")
                        {
                            if (urlsCrawled == 0)
                            {
                                //ParseRobots("http://www.cnn.com/robots.txt");
                                ParseRobots("http://bleacherreport.com/robots.txt");

                                CrawlerParser.disallowed = new HashSet<string>(disallowedLinks);

                                foreach (string xmlLink in xmlLinks)
                                {
                                    webParser.crawlXml(xmlLink);
                                }
                            }

                            Storage.StatusQueue.Clear();
                            CloudQueueMessage continueMsg = new CloudQueueMessage("crawl");
                            Storage.StatusQueue.AddMessage(continueMsg);
                        }
                        else if (statusMsg.AsString == "crawl")
                        {
                            if (Storage.UrlQueue.PeekMessage() != null)
                            {
                                CloudQueueMessage nextUrl = Storage.UrlQueue.GetMessage();
                                string nextUrlString = nextUrl.AsString;
                                string[] tableInfo = webParser.crawlHTML(nextUrlString); // { url, title, page date }
                                if (tableInfo != null) // Gets rid of repeat urls
                                {
                                    urlsCrawled++;
                                    // Update Last10
                                    if (Last10.Count == 10)
                                    {
                                        Last10.Dequeue();
                                    }
                                    Last10.Enqueue(nextUrlString);

                                    // If crawlHTML was successful, add to table
                                    if (tableInfo[0] != "")
                                    {
                                        addToTable(tableInfo);
                                        sitesindexed++;
                                    }
                                    // If crawlHTML failed, add to Errors list
                                    else
                                    {
                                        Errors.Add(nextUrlString);
                                    }
                                }

                                Storage.UrlQueue.DeleteMessage(nextUrl);
                                updateDashboardStats();
                            }
                        }
                        else if (statusMsg.AsString == "stop")
                        {
                            Thread.Sleep(2500);
                        }
                    }
                    else
                    {
                        // if status queue is cleared, reset the crawler

                        disallowedLinks = new HashSet<string>();
                        xmlLinks = new List<string>();

                        Last10 = new Queue<string>();
                        Errors = new List<string>();

                        urlsCrawled = 0;
                        sitesindexed = 0;
                        sizeOfIndex = 0;

                        webParser = new CrawlerParser();

                        Thread.Sleep(5000);
                    }
                }

                //this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public void ParseRobots(string urlIn)
        {
            WebClient wc = new WebClient();
            Stream file = wc.OpenRead(urlIn);
            StreamReader sr = new StreamReader(file);
            string line = "";

            while ((line = sr.ReadLine()) != null)
            {
                // XOR filters out BR sitemaps w/o "nba"
                if (line.StartsWith("Sitemap") && line.EndsWith(".xml") 
                    ^ (line.Contains("bleacherreport") && !line.Contains("nba")))
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
        }

        public void addToTable(string[] urlInfo)
        {
            // { url, title, page date }

            string str = urlInfo[1].ToLower();
            char[] arr = str.ToCharArray();

            arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
                                  || char.IsWhiteSpace(c))));

            string friendlyTitle = new string(arr);

            string[] splitTitle = friendlyTitle.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string encodedUrl = Storage.EncodeUrlInKey(urlInfo[0]);

            try
            {
                foreach (string keyword in splitTitle)
                {
                    UrlTE newIndex = new UrlTE(keyword, encodedUrl, urlInfo[1], urlInfo[2]);
                    TableOperation insertOperation = TableOperation.InsertOrReplace(newIndex);
                    Storage.UrlTable.ExecuteAsync(insertOperation);
                    sizeOfIndex++;
                }
            }
            catch
            {
                Errors.Add(urlInfo[0]);
            }
        }

        public void updateDashboardStats()
        {
            string cpu = theCPUCounter.NextValue().ToString();
            string ram = theMemCounter.NextValue().ToString();

            string last10String = buildDashTableString(Last10.ToList<string>()).Trim();
            string errorsString = buildDashTableString(Errors).Trim();

            DashTE newStats = new DashTE(cpu, ram, urlsCrawled, sizeOfIndex, last10String, errorsString);
            TableOperation updateStatsOperation = TableOperation.InsertOrReplace(newStats);
            Storage.StatsTable.ExecuteAsync(updateStatsOperation);
        }

        private string buildDashTableString(List<string> listIn)
        {
            string buildReturn = "";
            foreach(string url in listIn)
            {
                buildReturn += url + " ";
            }

            return buildReturn;
        }

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
