using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;

namespace Peeralize.Service.Integration.Blocks
{
    public class EntityDataImporter : IntegrationBlock
    {
        private string _inputFileName;
        private Func<string[], IntegratedDocument, bool> _matcher;
        public char Delimiter { get; set; }
        public CrossSiteAnalyticsHelper Helper { get; set; }
        private Action<string[], IntegratedDocument> _joiner;

        public EntityDataImporter(string inputFile, bool relative = false) : base()
        {
            Delimiter = ',';
            if (relative)
            {
                inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            }
            _inputFileName = inputFile;

        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            var matchingRow = FindMatchingEntry(intDoc);
            if (matchingRow != null)
            { 
                _joiner(matchingRow, intDoc); 
            }
            return intDoc;
        }

        private string[] FindMatchingEntry(IntegratedDocument doc)
        {
            if (_joiner == null)
            {
                throw new Exception("Data joiner is not set!");
            }
            using (var fs = File.Open(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var _reader = new StreamReader(fs);
                var _csvReader = new CsvReader(_reader, true, Delimiter);
                foreach (var row in _csvReader)
                {
                    var isMatch = _matcher(row, doc);
                    if (isMatch)
                    {
                        return row;
                    }
                }
            }
            return null;     
        }

        public void SetEntityRelation(Func<string[], IntegratedDocument, bool> func)
        {
            _matcher = func;
        }

        public void JoinOn(Action<string[], IntegratedDocument> joiner)
        {
            _joiner = joiner;
        }
    }
}