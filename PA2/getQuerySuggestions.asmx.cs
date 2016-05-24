using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Web.Script.Services;
using System.Diagnostics;
using System.Runtime;
using Microsoft.VisualBasic.Devices;

namespace PA2
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
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
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("njn344_AzureStorageConnectionString"));

            // Create a blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get a reference to a container named “mycontainer.”
            CloudBlobContainer container = blobClient.GetContainerReference("pa2");

            // Retrieve reference to a blob named "photo1.jpg".
            // CloudBlockBlob blockBlob = container.GetBlockBlobReference("ValidTitles_lowercase_nodigits.txt");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("ValidTitles_lowercase_nodigits.txt");

            // Save blob contents to a file.
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
        }
    }
}
