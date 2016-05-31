using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA4ClassLibrary
{
    public class DashTE : TableEntity
    {
        public DashTE(string cpu, string ram, int urlsCrawled, int sizeOfIndex, string lastTenString, string errorsString)
        {
            this.PartitionKey = "dashboard";
            this.RowKey = "stats";

            this.CPU = cpu;
            this.RAM = ram;
            this.UrlsCrawled = urlsCrawled;
            this.SizeOfIndex = sizeOfIndex;
            this.LastTen = lastTenString;
            this.Errors = errorsString;
        }

        public DashTE() { }

        public string CPU { get; set; }
        public string RAM { get; set; }
        public int UrlsCrawled { get; set; }
        public int SizeOfIndex { get; set; }
        public string LastTen { get; set; }
        public string Errors { get; set; }

        public string[] LastTenArr { get; set; }
        public string[] ErrorsArr { get; set; }

        public string TrieSize { get; set; }
        public string LastTitle { get; set; }
    }
}
