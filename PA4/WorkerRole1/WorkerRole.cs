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

// TODO: Change Table storage to map words in title to URL, instead of the current page URL 
//       to title/date.For example, if the title is “Microsoft goes IPO” then the key should 
//       be “microsoft”, “goes”, “ipo” and the value is the<URL, date> pair.This is a simplified inverted index.
// TODO: Only limit to last X months when parsing sitemap URLs
// TODO: Fix the crawler for bleacherreport.com.Due to the custom routing issue, we need to upgrade our 
//       crawler to handle BR.In the sitemap, we only load links in the nba xml. Then in the crawling 
//       phase -> we only consider links that follow this pattern http://*.bleacherreport.com/articles/*

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        // CPU
        //private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //this.theCPUCounter.NextValue();

        // RAM
        //private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        //this.theMemCounter.NextValue();

        private HashSet<string> disallowedLinks = new HashSet<string>();
        private List<string> xmlLinks = new List<string>();

        private List<string> tempLast10 = new List<string>();

        private int urlsCrawled = 0;
        private int sitesindexed = 0;
        private int tableSize = 0;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                WebParser webParser = new WebParser();
                CloudQueueMessage statusMsg;
                while(true)
                {
                    if ((statusMsg = Storage.StatusQueue.PeekMessage()) != null)
                    {
                        if (statusMsg.AsString == "load")
                        {
                            //ParseRobots("http://www.cnn.com/robots.txt");
                            ParseRobots("http://bleacherreport.com/robots.txt");

                            WebParser.disallowed = new HashSet<string>(disallowedLinks);

                            foreach(string xmlLink in xmlLinks)
                            {
                                webParser.crawlXml(xmlLink);
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

                                // Update Last10
                                if (Storage.Last10.Count == 10)
                                {
                                    Storage.Last10.Dequeue();
                                }
                                Storage.Last10.Enqueue(nextUrlString);

                                if (tableInfo != null)
                                {
                                    addToTable(tableInfo);
                                    sitesindexed++;
                                }

                                Storage.UrlQueue.DeleteMessage(nextUrl);
                                urlsCrawled++;
                            }
                        }
                        else if (statusMsg.AsString == "stop")
                        {
                            // do stop stuff
                        }
                        else if (statusMsg.AsString == "clear")
                        {
                            // do clear stuff
                        }
                    }
                    else
                    {
                        Thread.Sleep(2000);
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

            string encodedUrl = Storage.EncodeUrlInKey(urlInfo[0]);

            string[] splitTitle = urlInfo[1].Split(' ');

            try
            {
                foreach (string keyword in splitTitle)
                {
                    //string encodedKeyword = pa4Storage.EncodeUrlInKey(keyword);
                    urlTE newIndex = new urlTE(keyword, encodedUrl, urlInfo[1], urlInfo[2]);
                    TableOperation insertOperation = TableOperation.InsertOrReplace(newIndex);
                    Storage.UrlTable.Execute(insertOperation);
                    tableSize++;
                }
            }
            catch
            {
                Storage.Errors.Add(urlInfo[0]);
            }
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
