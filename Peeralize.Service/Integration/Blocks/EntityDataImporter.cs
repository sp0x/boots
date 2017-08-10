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
        private FileStream _fs;
        public List<string[]> Items { get; private set; }

        public EntityDataImporter(string inputFile, bool relative = false) : base()
        {
            Delimiter = ',';
            if (relative)
            {
                inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            }
            _inputFileName = inputFile;
            _fs = File.Open(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            Items = new List<string[]>();
            var _reader = new StreamReader(_fs);
            var _csvReader = new CsvReader(_reader, true, Delimiter);
            foreach (var row in _csvReader)
            {
                Items.Add(row);
            }
            _fs.Close();
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

        public IIntegrationDestination ContinueWith(Action<EntityDataImporter> action)
        {
            var completion = GetActionBlock().Completion;
            completion.ContinueWith(xTask =>
            {
                action(this);
            });
            return this;
        }

        private string[] FindMatchingEntry(IntegratedDocument doc)
        {
            if (_joiner == null)
            {
                throw new Exception("Data joiner is not set!");
            } 
            foreach (var row in Items)
            { 
                var isMatch = _matcher(row, doc);
                if (isMatch)
                {
                    return row;
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