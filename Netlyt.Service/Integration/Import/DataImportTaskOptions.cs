using System;
using System.Collections.Generic;
using nvoid.Integration;
using Netlyt.Service.IntegrationSource;

namespace Netlyt.Service.Integration.Import
{
    public class DataImportTaskOptions
    {
        public uint ReadBlockSize { get; set; } = 30000;
        public InputSource Source { get; set; }
        public ApiAuth ApiKey { get; set; }
        public string TypeName { get; set; }
        public uint ThreadCount { get; set; } = 10;
        public List<String> IndexesToCreate { get; set; }

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