using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using PA4ClassLibrary;

namespace WebRole1
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        private JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

        public static Trie thisTrie;
        public static string blobReturn;
        public static string trieReturn;

        private string trieSize = "";
        private string lastTitle = "";

        private static Dictionary<string, Tuple<List<Tuple<string, string>>, DateTime>> cache = 
            new Dictionary<string, Tuple<List<Tuple<string, string>>, DateTime>>();

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string queryTableStorage(string tsQuery)
        {
            if (cache == null) // if cache is null, create new
            {
                cache = new Dictionary<string, Tuple<List<Tuple<string, string>>, DateTime>>();
            }

            // check if in cache and if cache entry isn't over 10 min old
            if (cache.ContainsKey(tsQuery) && (cache[tsQuery].Item2 > (DateTime.Now.Subtract(new TimeSpan(0, 10, 0)))))
            {
                // update DateTime
                cache[tsQuery] = Tuple.Create(cache[tsQuery].Item1, DateTime.Now);
                return jsSerializer.Serialize(cache[tsQuery].Item1);
            }
            else
            {
                // Want to keep unique list of <url, page title> so that we don't process a site
                //   multiple time. This key maps to another Tuple respresent rank (int) and page date (DateTime)
                Dictionary<Tuple<string, string>, Tuple<int, DateTime>> possibleResults = 
                    new Dictionary<Tuple<string, string>, Tuple<int, DateTime>>();

                char[] arr = tsQuery.ToLower().ToCharArray();
                arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
                                      || char.IsWhiteSpace(c))));

                string friendlyQuery = new string(arr);
                string[] splitQuery = friendlyQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                //HashSet<string> queryHashSet = new HashSet<string>(splitQuery);

                foreach (string queryKeyword in splitQuery)
                {
                    TableQuery<UrlTE> tableQuery = new TableQuery<UrlTE>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, queryKeyword));
                    foreach (UrlTE returnedRow in Storage.UrlTable.ExecuteQuery(tableQuery))
                    {
                        string friendlyUrl = Storage.DecodeUrlInKey(returnedRow.RowKey);
                        var newKey = Tuple.Create(friendlyUrl, returnedRow.FullTitle);
                        if (!possibleResults.ContainsKey(newKey))
                        {
                            char[] titleArr = newKey.Item2.ToLower().ToCharArray();

                            titleArr = Array.FindAll<char>(titleArr, (c => (char.IsLetterOrDigit(c)
                                                  || char.IsWhiteSpace(c))));

                            string friendlyTitle = new string(titleArr);

                            string[] splitTitle = friendlyTitle.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            //int rank = splitTitle.Where(x => queryHashSet.Contains(x)).Count();
                            int rank = splitQuery.Intersect(splitTitle).Count();

                            possibleResults.Add(newKey, Tuple.Create(rank, DateTime.Parse(returnedRow.Date)));
                        }
                    }
                }

                List<Tuple<string, string>> top20 = possibleResults.OrderByDescending(x => x.Value.Item1)
                                                                    .ThenByDescending(x => x.Value.Item2)
                                                                    .Select(x => x.Key).Take(20).ToList();

                // if cache is full, clear it
                if (cache.Count >= 100)
                {
                    cache.Clear();
                }

                cache[tsQuery] = Tuple.Create(top20, DateTime.Now);

                return jsSerializer.Serialize(top20);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string downloadWiki()
        {
            string filePath = Path.GetTempPath() + "\\wiki.txt";
            using (var fileStream = File.OpenWrite(filePath))
            {
                Storage.BlockBlob.DownloadToStream(fileStream);
            }

            return "success";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string buildTrie()
        {
            PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");

            string last = "";
            int totalCount = 0;
            List<string> wikiTitles = new List<string>();
            string wikiPath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            wikiTitles = System.IO.File.ReadAllLines(wikiPath).ToList();

            string mbLeft = "";

            thisTrie = new Trie();
            foreach (string title in wikiTitles)
            {
                if (totalCount > 1000000 && (totalCount % 1000 == 0))
                {
                    if (theMemCounter.NextValue() < 25)
                    {
                        mbLeft = theMemCounter.NextValue().ToString();
                        last = title;
                        break;
                    }
                }
                thisTrie.addTitle(title);
                totalCount++;
            }

            trieSize = totalCount.ToString();
            lastTitle = last;

            return "total: " + totalCount + ", last: " + last + ", Mem left: " + mbLeft;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string trieQuery)
        {
            if (trieQuery.Length == 0)
            {
                return "";
            }
            // query -> lowercase, trim, and replace spaces with underscore
            return jsSerializer.Serialize(thisTrie.searchForPrefix(trieQuery.ToLower().Trim().Replace(" ", "_")));
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string startCrawling()
        {
            Storage.StatusQueue.Clear();
            CloudQueueMessage startMsg = new CloudQueueMessage("load");
            Storage.StatusQueue.AddMessage(startMsg);

            return "Started crawling.";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string stopCrawling()
        {
            Storage.StatusQueue.Clear();
            CloudQueueMessage stopMsg = new CloudQueueMessage("stop");
            Storage.StatusQueue.AddMessage(stopMsg);

            return "Stopped crawling.";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string clearCrawler()
        {
            Storage.StatusQueue.Clear();
            Storage.UrlQueue.Clear();
            Storage.UrlTable.DeleteIfExists();
            Storage.StatsTable.DeleteIfExists();

            return "Everything cleared.";
        }

        private string getState()
        {
            if (Storage.StatusQueue.Exists() && Storage.StatusQueue.PeekMessage() != null)
            {
                return Storage.StatusQueue.PeekMessage().AsString;
            }
            else
            {
                return "empty";
            }
        }

        private string getQueueSize()
        {
            if (Storage.UrlQueue.Exists())
            {
                Storage.UrlQueue.FetchAttributes();
                int? count = Storage.UrlQueue.ApproximateMessageCount;
                return count.ToString();
            }
            else
            {
                return "0";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getAllStats()
        {
            DashTE statsRow = ((from entity in Storage.StatsTable.CreateQuery<DashTE>()
                                where entity.PartitionKey == "dashboard"
                                && entity.RowKey == "stats"
                                select entity).Take(1)).First();

            statsRow.LastTenArr = statsRow.LastTen.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            statsRow.ErrorsArr = statsRow.Errors.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            statsRow.State = getState();
            statsRow.QueueSize = getQueueSize();

            statsRow.TrieSize = trieSize;
            statsRow.LastTitle = lastTitle;

            return jsSerializer.Serialize(statsRow);
        }
    }
}
