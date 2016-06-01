using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4ClassLibrary
{
    public static class Storage
    {

        public static CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);

        public static CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
        public static CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
        public static CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();

        public static CloudTable UrlTable = tableClient.GetTableReference("pafurltable");
        public static CloudTable StatsTable = tableClient.GetTableReference("pafstatstable");

        public static CloudQueue UrlQueue = queueClient.GetQueueReference("pafurlqueue");
        public static CloudQueue StatusQueue = queueClient.GetQueueReference("pafstatusqueue");

        public static CloudBlobContainer container = blobClient.GetContainerReference("container");
        public static CloudBlockBlob BlockBlob = container.GetBlockBlobReference("ValidTitles_lowercase_nodigits.txt");

        static Storage()
        {
            UrlTable.CreateIfNotExists();
            StatsTable.CreateIfNotExists();

            UrlQueue.CreateIfNotExists();
            StatusQueue.CreateIfNotExists();
        }

        public static string EncodeUrlInKey(string url)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = System.Convert.ToBase64String(keyBytes);
            return base64.Replace('/', '_');
        }

        public static string DecodeUrlInKey(string encodedKey)
        {
            var base64 = encodedKey.Replace('_', '/');
            byte[] bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
