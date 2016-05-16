﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA3ClassLibrary
{
    public class StatusTableEntity : TableEntity
    {
        public StatusTableEntity(string message)
        {
            this.PartitionKey = DateTime.Now.ToString();
            this.RowKey = message;
        }

        public StatusTableEntity() { }

    }
}
