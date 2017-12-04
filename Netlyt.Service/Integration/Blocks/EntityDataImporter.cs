using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityDataImporter 
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
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

        public EntityDataImporter(string inputFile, bool relative = false, bool map = false) 
            : base(procType: BlockType.Transform)
        {
            Delimiter = ',';
            if (relative)
            {
                inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            }
            _inputFileName = inputFile;
            MappedItems = new Dictionary<string, string[]>();
            if (map) ReadData();
        }

        /// <summary>
        /// Reads the input, mapping it with the input mapper.
        /// </summary>
        public void ReadData()
        {
            _fs = File.Open(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CacheItems = new List<string[]>();
            var _reader = new StreamReader(_fs);
            var _csvReader = new CsvReader(_reader, true, Delimiter);
            
            if (_inputMapper != null)
            { 
                foreach (var row in _csvReader)
                { 
                    MappedItems[_inputMapper(row)] = row;
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

        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return null;
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

        public IFlowBlock ContinueWith(Action<EntityDataImporter> action)
        {
            var completion = GetProcessingBlock().Completion;
            completion.ContinueWith(xTask =>
            {
                MappedItems.Clear();
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="joiner"></param>
        public void JoinOn(Action<string[], IntegratedDocument> joiner)
        {
            _joiner = joiner;
        }

        public void UseInputKey(Func<string[], string> func)
        {
            _inputMapper = func;
        }

        public void SetEntityKey(Func<IntegratedDocument, string> func)
        {
            _entityKeyResolver = func;
        }
    }
}