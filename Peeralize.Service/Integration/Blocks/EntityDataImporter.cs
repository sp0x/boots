using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;

namespace Peeralize.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityDataImporter : IntegrationBlock
    {
        private string _inputFileName;
        private Func<string[], IntegratedDocument, bool> _matcher;
        public char Delimiter { get; set; }
        public CrossSiteAnalyticsHelper Helper { get; set; }
        private Action<string[], IntegratedDocument> _joiner;
        private Func<string[], string> _inputMapper;
        private FileStream _fs;
        private Func<IntegratedDocument, string> _entityKeyResolver;
        public List<string[]> CacheItems { get; private set; }
        public Dictionary<string, string[]> MappedItems { get; private set; }

        public EntityDataImporter(string inputFile, bool relative = false, bool map = false) : base()
        {
            Delimiter = ',';
            if (relative)
            {
                inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            }
            _inputFileName = inputFile;
            MappedItems = new Dictionary<string, string[]>();
            if (map) Map();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Map()
        {
            _fs = File.Open(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CacheItems = new List<string[]>();
            var _reader = new StreamReader(_fs);
            var _csvReader = new CsvReader(_reader, true, Delimiter);
            
            if (_inputMapper != null)
            { 
                foreach (var row in _csvReader)
                {
                    var key = _inputMapper(row);
                    MappedItems[key] = row;
                }
            }
            else
            {
                foreach (var row in _csvReader)
                {
                    CacheItems.Add(row);
                }
            }
            _fs.Close();
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            string[] matchingRow = null;
            if (_entityKeyResolver != null && MappedItems.Count > 0)
            {
                var entityKey = _entityKeyResolver(intDoc);
                matchingRow = MappedItems.ContainsKey(entityKey) ? MappedItems[entityKey] : null;
            }
            else
            {
                matchingRow = FindMatchingEntry(intDoc);
            }
            if (matchingRow != null)
            { 
                _joiner(matchingRow, intDoc); 
            }
            return intDoc;
        }

        public IIntegrationDestination ContinueWith(Action<EntityDataImporter> action)
        {
            var completion = GetProcessingBlock().Completion;
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
            foreach (var row in CacheItems)
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

        public void SetDataKey(Func<string[], string> func)
        {
            _inputMapper = func;
        }

        public void SetEntityKey(Func<IntegratedDocument, string> func)
        {
            _entityKeyResolver = func;
        }
    }
}