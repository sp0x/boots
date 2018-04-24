using System;
using System.Collections.Generic;
using System.Text;
using nvoid.exec.Blocks;
using Netlyt.Interfaces;

namespace Netlyt.Service.Integration.Blocks
{
    public class IntegrationActionBlock<T> : BaseFlowBlock<T, T>
        where T : class, IIntegratedDocument
    {
        private Func<IntegrationActionBlock<T>, T, T> _action; 

        public IntegrationActionBlock(string appId, Action<IntegrationActionBlock<T>, T> action, int threadCount = 4)
            :base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            this.AppId = appId;
            _action = ((act, x)=>
            {
                action(act, x);
                return x;
            });
        }

        public IntegrationActionBlock(Action<IntegrationActionBlock<T>, T> action, int threadCount = 4)
            : base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            _action = ((act, x) =>
            {
                action(act, x);
                return x;
            });
        }

        public IntegrationActionBlock(string appId, Func<IntegrationActionBlock<T>, T, T> action, int threadCount = 4)
            : base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            this.AppId = appId;
            _action = action;
        }

        public IntegrationActionBlock(string appId, Action<IntegrationActionBlock<T>, T> action)
            : base(capacity: 100000, procType: BlockType.Action, threadCount : 4)
        {
            AppId = appId;
            _action = ((act, x) =>
            {
                action(act, x);
                return x;
            });
        }

        protected override IEnumerable<T> GetCollectedItems()
        {
            return null;
        }

        protected override T OnBlockReceived(T intDoc)
        {
            var output = _action(this, intDoc);
            return output;
        }
    }
}
