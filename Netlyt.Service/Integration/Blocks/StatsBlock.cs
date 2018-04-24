using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using nvoid.exec.Blocks;
using Netlyt.Interfaces;

namespace Netlyt.Service.Integration.Blocks
{
    public class StatsBlock<T> : BaseFlowBlock<T, T>
        where T : class, IIntegratedDocument
    {
        private Action<T> _action;
        private BlockingCollection<T> _items;
        private object _lock;

        public StatsBlock(Action<T> action)
            : base(procType: BlockType.Action, threadCount: 4)
        {
            _action = action;
            _items = new BlockingCollection<T>();
            _lock = new object();
        }


        protected override IEnumerable<T> GetCollectedItems()
        {
            return _items;
        }

        protected override T OnBlockReceived(T intDoc)
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
                _items = new BlockingCollection<T>();
            }
        }

    }
}
