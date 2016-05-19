using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA3ClassLibrary
{
    public class urlTableEntity : TableEntity
    {
        public urlTableEntity(string url, string title, string date)
        {
            this.PartitionKey = string.Format("{0:d19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = url;

            this.Title = title;
            this.PageDate = date;
        }

        public urlTableEntity() { }

        public string Title { get; set; }
        public string PageDate { get; set; }
    }
}
