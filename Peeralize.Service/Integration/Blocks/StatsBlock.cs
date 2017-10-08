using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Peeralize.Service.Integration.Blocks
{
    public class StatsBlock
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        private Action<IntegratedDocument> _action;
        private BlockingCollection<IntegratedDocument> _items;
        private object _lock;

        public StatsBlock(Action<IntegratedDocument> action)
            : base(procType: ProcessingType.Action, threadCount: 4)
        {
            _action = action;
            _items = new BlockingCollection<IntegratedDocument>();
            _lock = new object();
        }


        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return _items;
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            _action(intDoc);
            _items.Add(intDoc);
            return intDoc;
        }

        public void Clear()
        {
            lock (_lock)
            { 
                _items.Dispose();
                _items = new BlockingCollection<IntegratedDocument>();
            }
        }

    }
}
