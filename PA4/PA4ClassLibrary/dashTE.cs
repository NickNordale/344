using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4ClassLibrary
{
    class dashTE : TableEntity
    {
        public dashTE(string cpu, string ram, int urlCount, int tableCount)
        {
            this.PartitionKey = "dashboard";
            this.RowKey = "data";
        }

        public dashTE() { }
    }
}
