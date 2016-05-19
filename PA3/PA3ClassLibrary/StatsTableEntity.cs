using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA3ClassLibrary
{
    public class StatsTableEntity : TableEntity
    {
        public StatsTableEntity(string errorUrl, string cpu, string ram, int urlCount, int tableCount)
        {
            this.PartitionKey = string.Format("{0:d19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = "stats";

            this.ErrorUrl = errorUrl;
            this.CPU = cpu;
            this.RAM = ram;
            this.UrlCount = urlCount;
            this.TableCount = tableCount;
        }

        public StatsTableEntity() { }

        public string ErrorUrl { get; set; }
        public string CPU { get; set; }
        public string RAM { get; set; }
        public int UrlCount { get; set; }
        public int TableCount { get; set; }
    }
}
