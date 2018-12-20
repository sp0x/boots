using System;
using System.Collections.Generic;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut.Data
{
    public class DataImportTaskOptions
    {
        public uint ReadBlockSize { get; set; } = 30000;
        public IInputSource Source { get; set; }
        public ApiAuth ApiKey { get; set; }
        public string IntegrationName { get; set; } 
        public DataIntegration Integration { get; set; }
        public uint ThreadCount { get; set; } = 10;
        public List<String> IndexesToCreate { get; set; }
        public uint ShardLimit { get; set; }
        public uint TotalEntryLimit { get; set; }
        public bool EncodeInput { get; set; }
        public DataImportTaskOptions()
        {
            IndexesToCreate = new List<string>();
        }

        public DataImportTaskOptions AddIndex(string index)
        {
            IndexesToCreate.Add(index);
            return this;
        }
    }
}