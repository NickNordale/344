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

// TODO: Query PA1 API for NBA players
// TODO: Query table storage for words in website titles
// TODO: Add code to rank results (LINQ statement)
// TODO: Add query suggestion admin stats to Dashboard (#titles & last title)

// TODO: (Fix) Memory handling in PA2 code
// TODO: (Fix) OOP Trie in PA2 code (private root node)

namespace WebRole1
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        // CPU
        //private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //this.theCPUCounter.NextValue();

        // RAM
        //private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        //this.theMemCounter.NextValue();

        public static Trie thisTrie;
        public static string blobReturn;
        public static string trieReturn;

        // No webmethod to facilitate NBA PA1 request right?

        /*[WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string queryTableStorage()
        {
            return "";
        }*/

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string downloadwiki()
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

            return "Last title: " + last + ", Total added: " + totalCount;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string query)
        {
            if (query.Length == 0)
            {
                return "";
            }
            return thisTrie.searchForPrefix(query);
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
            CloudQueueMessage startMsg = new CloudQueueMessage("stop");
            Storage.StatusQueue.AddMessage(startMsg);

            return "Stopped crawling.";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string checkLastTen()
        {
            string o = "";
            List<string> copyQueue = Storage.Last10.ToList<string>();
            foreach(string s in copyQueue)
            {
                o += s + ", ";
            }
            return o;
        }
    }
}
