using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
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
using System.Xml.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Reflection;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);

        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable urlTable = tableClient.GetTableReference("urlTable");

        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue startStopQueue = queueClient.GetQueueReference("startstop");

        private HashSet<string> crawledUrls = new HashSet<string>();
        private HashSet<string> crawledXmls = new HashSet<string>();
        private HashSet<string> disallowedLinks = new HashSet<string>();

        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        private static CloudTable statsTable = tableClient.GetTableReference("statsTable");

        private List<string> newErrors = new List<string>();

        private int countUrls = 0;
        private int tableSize = 0;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            Thread.Sleep(50);

            CloudQueueMessage statusMsg;
            CloudQueueMessage nextUrl;

            startStopQueue.CreateIfNotExists();

            while ((statusMsg = startStopQueue.PeekMessage()) != null && 
                (statusMsg = startStopQueue.PeekMessage()).AsString.Contains("start"))
            {
                if (statusMsg.AsString == "start init")
                {
                    //string cnnRobotsUrl = "http://www.cnn.com/robots.txt";
                    //List<string> cnnXml = ParseRobots(cnnRobotsUrl);

                    /*foreach (string cnnUrl in cnnXml)
                    {
                        crawlXml(cnnUrl);
                        crawledXmls.Add(cnnUrl);
                    }*/

                    string brRobotsUrl = "http://bleacherreport.com/robots.txt";
                    List<string> brXml = ParseRobots(brRobotsUrl);

                    foreach (string brUrl in brXml)
                    {
                        if (brUrl.EndsWith("/nba.xml"))
                        {
                            crawlXml(brUrl);
                            crawledXmls.Add(brUrl);
                        }
                    }

                    startStopQueue.Clear();
                    CloudQueueMessage continueMsg = new CloudQueueMessage("start");
                    startStopQueue.AddMessage(continueMsg);
                }
                else
                {
                    if (urlQueue.Exists() && (nextUrl = urlQueue.GetMessage()) != null)
                    {
                        // robots done, crawl normally
                        string nextUrlString = nextUrl.AsString;
                        urlQueue.DeleteMessage(nextUrl);
                        crawl(nextUrlString);
                    }
                    updateStats();
                }
            }
        }

        public List<string> ParseRobots(string urlIn)
        {
            WebClient wc = new WebClient();
            Stream file = wc.OpenRead(urlIn);
            StreamReader sr = new StreamReader(file);
            List<string> xmlLinks = new List<string>();
            string line = "";

            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("Sitemap") && line.Trim().EndsWith(".xml"))
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

            return xmlLinks;
        }

        public void crawlXml(string xmlPassed)
        {
            if (!crawledXmls.Contains(xmlPassed))
            {
                XNamespace ns = xmlPassed;
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(xmlPassed);

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    if (node["loc"].InnerText.EndsWith(".xml"))
                    {
                        if (node["lastmod"] != null)
                        {
                            string nodeLastMod = node["lastmod"].InnerText;
                            int year = Int32.Parse(nodeLastMod.Substring(0, 4));
                            int month = Int32.Parse(nodeLastMod.Substring(5, 2));
                            int day = Int32.Parse(nodeLastMod.Substring(8, 2));
                            DateTime dt = new DateTime(year, month, day);
                            if ((DateTime.Compare(dt, new DateTime(2016, 3, 1))) > 0)
                            {
                                crawlXml(node["loc"].InnerText);
                                crawledXmls.Add(node["loc"].InnerText);
                            }
                        }
                        else
                        {
                            crawlXml(node["loc"].InnerText);
                            crawledXmls.Add(node["loc"].InnerText);
                        }
                    }
                    else
                    {
                        if (checkLink(node["loc"].InnerText))
                        {
                            CloudQueueMessage newLinkMsg = new CloudQueueMessage(node["loc"].InnerText);
                            urlQueue.AddMessageAsync(newLinkMsg);
                        }
                    }
                }
            }
        }

        public void crawl(string crawlPassed)
        {
            if (!crawledUrls.Contains(crawlPassed))
            {
                crawledUrls.Add(crawlPassed);
                countUrls++;

                try
                {
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument doc = web.Load(crawlPassed);

                    if (doc.DocumentNode.SelectNodes("//a[@href]") != null)
                    {
                        foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                        {
                            string absUrl = GetAbsoluteUrlString(crawlPassed, link.Attributes["href"].Value);
                            if (checkLink(absUrl))
                            {
                                CloudQueueMessage newMessage = new CloudQueueMessage(absUrl);
                                urlQueue.AddMessageAsync(newMessage);
                            }
                        }
                    }

                    var title = doc.DocumentNode.SelectSingleNode("//title[1]");
                    string titleString = "";
                    if (title != null)
                    {
                        titleString = title.InnerText;
                    }
                    else
                    {
                        return;
                    }

                    string timeString = "";

                    if (crawlPassed.Contains("cnn.com") && doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]") != null &&
                        doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]").Attributes["content"] != null)
                    {
                        timeString = doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate'][1]").Attributes["content"].Value;
                    }
                    else if (crawlPassed.Contains("bleacherreport.com") && doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]") != null &&
                        doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]").Attributes["content"] != null)
                    {
                        timeString = doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate'][1]").Attributes["content"].Value;
                    }

                    if (timeString != null && timeString != "")
                    {
                        DateTime pageDT = Convert.ToDateTime(timeString);
                        timeString = pageDT.ToString();
                    }
                    else
                    {
                        timeString = DateTime.Now.ToString();
                    }
                    insertTable(crawlPassed, titleString, timeString);
                }
                catch
                {
                    newErrors.Add(crawlPassed);
                }
            }
        }

        static string GetAbsoluteUrlString(string baseUrl, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri(baseUrl), uri);
            return uri.ToString();
        }

        public Boolean checkLink(string checkUrl)
        {
            // check http and contains either cnn or br domain
            if (!checkUrl.StartsWith("http://") || !(checkUrl.Contains(".cnn.com") || checkUrl.Contains("bleacherreport.com")))
            {
                return false;
            }

            // check if disallowed
            string relativePath = checkUrl.Substring(checkUrl.IndexOf(".com") + 4);
            string startDir = "";
            if (relativePath.Substring(1).Contains("/"))
            {
                startDir = relativePath.Substring(0, (relativePath.Substring(1).IndexOf("/") + 1));
            }
            else
            {
                startDir = relativePath;
            }

            if (disallowedLinks.Contains(startDir))
            {
                return false;
            }

            // check if already visited
            if (crawledUrls.Contains(checkUrl))
            {
                return false;
            }

            // passed all tests
            return true;
        }

        public void insertTable(string url, string title, string date)
        {
            string encodedUrl = EncodeUrlInKey(url);
            string result = Regex.Replace(title, @"\r\n?|\n", "*");
            string encodedTitle = Regex.Replace(result, @"[?\\\/#]", "*");

            urlTableEntity insertUrl = new urlTableEntity(encodedUrl, encodedTitle, date);
            TableOperation insertOperation = TableOperation.InsertOrReplace(insertUrl);
            urlTable.ExecuteAsync(insertOperation);
            tableSize++;
        }

        public void updateStats()
        {
            string cpu = cpuCounter.NextValue().ToString();
            string ram = ramCounter.NextValue().ToString();

            string errorString = "";
            foreach (string s in newErrors)
            {
                errorString += s;
                errorString += " ";
            }

            StatsTableEntity newStats = new StatsTableEntity(errorString, cpu, ram, countUrls, tableSize);
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(newStats);
            statsTable.Execute(insertOrReplaceOperation);

            newErrors.Clear();
        }

        private static String EncodeUrlInKey(String url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
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
