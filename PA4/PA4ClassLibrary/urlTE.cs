using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4ClassLibrary
{
    class urlTE : TableEntity
    {
        public urlTE(string term, string url)
        {
            this.PartitionKey = term;
            this.RowKey = url;
        }

        public urlTE() { }
    }
}
