using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class UrlTE : TableEntity
    {
        public UrlTE(string term, string url, string fullTitle, string date)
        {
            this.PartitionKey = term;
            this.RowKey = url;

            this.FullTitle = fullTitle;
            this.Date = date;
        }

        public UrlTE() { }

        public string FullTitle { get; set; }
        public string Date { get; set; }
    }
}
