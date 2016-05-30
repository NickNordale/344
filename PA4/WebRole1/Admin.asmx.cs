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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string queryTableStorage(string query)
        {
            Dictionary<Tuple<String, String>, int> possibleResults = new Dictionary<Tuple<String, String>, int>();

            char[] arr = query.ToLower().ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
                                  || char.IsWhiteSpace(c))));

            string friendlyQuery = new string(arr);
            string[] splitQuery = friendlyQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //HashSet<string> queryHashSet = new HashSet<string>(splitQuery);

            foreach (string queryKeyword in splitQuery)
            {
                TableQuery<UrlTE> tableQuery = new TableQuery<UrlTE>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, queryKeyword));
                foreach(UrlTE returnedRow in Storage.UrlTable.ExecuteQuery(tableQuery))
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

                        possibleResults.Add(newKey, rank);
                    }
                }
            }

            var sortedResults = from entity in possibleResults orderby entity.Value descending select entity;

            List<Tuple<string, string>> top20 = new List<Tuple<string, string>>();

            for (int i = 0; i < 20 && i < sortedResults.Count() - 1; i++)
            {
                top20.Add(sortedResults.ElementAt(i).Key);
            }

            return jsSerializer.Serialize(top20);
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
            using (StreamReader sr = new StreamReader(wikiPath))
            {
                while (sr.EndOfStream == false)
                {
                    string line = sr.ReadLine();
                    wikiTitles.Add(line);
                }
            }
            thisTrie = new Trie();
            foreach (string title in wikiTitles)
            {
                if (theMemCounter.NextValue() < 50)
                {
                    last = title;
                    break;
                }
                thisTrie.addTitle(title);
                totalCount++;
            }

            trieSize = totalCount.ToString();
            lastTitle = last;

            return "success";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string query)
        {
            if (query.Length == 0)
            {
                return "";
            }
            // query -> lowercase, trim, and replace spaces with underscore
            return jsSerializer.Serialize(thisTrie.searchForPrefix(query.ToLower().Trim().Replace(" ", "_")));
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

        public string getState()
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

        public string getQueueSize()
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
