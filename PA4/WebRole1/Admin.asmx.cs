using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

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
        // No webmethod to facilitate NBA PA1 request right?
        /*[WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void queryNBAPlayers(string query, string callback)
        {
            
        }*/

        /*[WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string queryTableStorage()
        {
            return "";
        }

        public static Trie thisTrie;
        public static string blobReturn;
        public static string trieReturn;

        static WebService1()
        {
            if (blobReturn != "success")
            {
                blobReturn = downloadwiki();
            }
            if (trieReturn != "success")
            {
                trieReturn = buildTrie();
            }
        }

        public static Boolean CanGetMemory()
        {
            try
            {
                MemoryFailPoint mfp = new MemoryFailPoint(50);
            }
            catch (InsufficientMemoryException)
            {
                return false;
            }
            return true;
        }

        [WebMethod]
        public static string downloadwiki()
        {
            string filePath = Path.GetTempPath() + "\\wiki.txt";
            using (var fileStream = File.OpenWrite(filePath))
            {
                blockBlob.DownloadToStream(fileStream);
            }

            return "success";
        }

        [WebMethod]
        public static string buildTrie()
        {
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
            int titleCount = 0;
            foreach (string title in wikiTitles)
            {
                if (titleCount >= 1000)
                {
                    if (!CanGetMemory())
                    {
                        return "last: " + last + ", count: " + totalCount;
                    }
                    totalCount += titleCount;
                    titleCount = 0;
                }
                thisTrie.addTitle(title);
                last = title;
                titleCount++;
            }
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
            return thisTrie.searchForPrefix(query);
        }*/
    }
}
