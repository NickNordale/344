using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4ClassLibrary
{
    public class urlTE : TableEntity
    {
        public urlTE(string term, string url, string fullTitle, string date)
        {
            this.PartitionKey = term;
            this.RowKey = url;

            this.FullTitle = fullTitle;
            this.Date = date;
        }

        public urlTE() { }

        public string FullTitle { get; set; }
        public string Date { get; set; }
    }
}
