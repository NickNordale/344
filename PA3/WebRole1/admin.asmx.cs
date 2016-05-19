using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using PA3ClassLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
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
        private static CloudTable urlTable = tableClient.GetTableReference("urlTable");

        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue startStopQueue = queueClient.GetQueueReference("startstop");

        private static CloudTable statsTable = tableClient.GetTableReference("statsTable");

        private List<string> errorList = new List<string>();

        private JavaScriptSerializer js = new JavaScriptSerializer();

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string startCrawling()
        {
            if (!startStopQueue.Exists() || startStopQueue.PeekMessage() == null)
            {
                startStopQueue.CreateIfNotExists();
                startStopQueue.Clear();
                urlTable.CreateIfNotExists();
                urlQueue.CreateIfNotExists();
                statsTable.CreateIfNotExists();

                CloudQueueMessage startMsg = new CloudQueueMessage("start init");
                startStopQueue.AddMessage(startMsg);
            }
            else
            {
                startStopQueue.CreateIfNotExists();
                startStopQueue.Clear();

                CloudQueueMessage startMsg = new CloudQueueMessage("start");
                startStopQueue.AddMessage(startMsg);
            }

            return "Started crawling.";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string stopCrawling()
        {
            startStopQueue.CreateIfNotExists();
            startStopQueue.Clear();
            CloudQueueMessage stopMsg = new CloudQueueMessage("stop");
            startStopQueue.AddMessage(stopMsg);

            return "Stopped crawling.";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string clearEverything()
        {
            startStopQueue.DeleteIfExists();
            urlQueue.DeleteIfExists();
            urlTable.DeleteIfExists();
            statsTable.DeleteIfExists();
            return "";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string search(string url)
        {
            if (urlTable.Exists())
            {
                string urlKey = EncodeUrlInKey(url);
                TableQuery<urlTableEntity> urlQuery = new TableQuery<urlTableEntity>()
                    .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, urlKey));
                var res = urlTable.ExecuteQuery(urlQuery).FirstOrDefault();
                if (res != null)
                {
                    return js.Serialize(res);
                }
                else
                {
                    return "Not found";
                }
            }
            else
            {
                return "Crawler has not indexed any pages yet.";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getData()
        {
            if (statsTable.Exists())
            {
                var stats = statsTable.ExecuteQuery(new TableQuery<StatsTableEntity>().Take(1));
                StatsTableEntity getStats = stats.FirstOrDefault();

                if (getStats != null)
                {
                    if (getStats.ErrorUrl != "")
                    {
                        string[] newErrors = getStats.ErrorUrl.Split(' ');
                        foreach (string s in newErrors)
                        {
                            errorList.Add(DecodeUrlInKey(s));
                        }
                    }
                    return js.Serialize(getStats);
                }
                else
                {
                    return "ERROR";
                }
            }
            else
            {
                return "No stats yet.";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getState()
        {
            if (startStopQueue.Exists() && startStopQueue.PeekMessage() != null)
            {
                if (startStopQueue.PeekMessage().AsString == "start init")
                {
                    return "Loading";
                }
                else if (startStopQueue.PeekMessage().AsString == "start")
                {
                    return "Crawling";
                }
                else
                {
                    return "Stopped";
                }
            }
            else
            {
                return "Not yet started crawling.";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getQueueSize()
        {
            if (urlQueue.Exists())
            {
                urlQueue.FetchAttributes();
                int? count = urlQueue.ApproximateMessageCount;
                return count.ToString();
            }
            else
            {
                return "0";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getLast10()
        {
            if (urlTable.Exists())
            {
                List<string> last10 = new List<string>();
                TableQuery<urlTableEntity> getLast10 = new TableQuery<urlTableEntity>().Take(10);
                foreach (urlTableEntity curr in urlTable.ExecuteQuery(getLast10))
                {
                    last10.Add(DecodeUrlInKey(curr.RowKey));
                }
                return js.Serialize(last10);
            }
            else
            {
                return "Crawler has not indexed any pages yet.";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getErrors()
        {
            return js.Serialize(errorList);
        }

        private static String EncodeUrlInKey(String url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

        private static String DecodeUrlInKey(String encodedKey)
        {
            var base64 = encodedKey.Replace('_', '/');
            byte[] bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
