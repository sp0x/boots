using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Integration.Blocks
{
    public class IntegrationActionBlock : IntegrationBlock
    {
        private Func<IntegrationBlock, IntegratedDocument, IntegratedDocument> _action;

        public IntegrationActionBlock(string userId, Action<IntegrationBlock, IntegratedDocument> action, int threadCount = 4)
            :base(capacity: 100000, procType: ProcessingType.Action, threadCount: threadCount)
        {
            this.UserId = userId;
            _action = new Func<IntegrationBlock, IntegratedDocument, IntegratedDocument>((act, x)=>
            {
                action(act, x);
                return x;
            });
        }
        public IntegrationActionBlock(string userId, Func<IntegrationBlock, IntegratedDocument, IntegratedDocument> action, int threadCount = 4)
            : base(capacity: 100000, procType: ProcessingType.Action, threadCount: threadCount)
        {
            this.UserId = userId;
            _action = action;
        }

        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return null;
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            var output = _action(this, intDoc);
            return output;
        }
    }
}
