﻿using Microsoft.WindowsAzure.Storage.Table;
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
            this.PartitionKey = url;
            this.RowKey = title;

            this.Date = date;
        }

        public urlTableEntity() { }

        public string Date { get; set; }
    }
}