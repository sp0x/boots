﻿using System;
using System.Collections.Generic;
using System.Text;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    public class IntegrationActionBlock 
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        private Func<IntegrationActionBlock, IntegratedDocument, IntegratedDocument> _action; 

        public IntegrationActionBlock(string userId, Action<IntegrationActionBlock, IntegratedDocument> action, int threadCount = 4)
            :base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            this.UserId = userId;
            _action = ((act, x)=>
            {
                action(act, x);
                return x;
            });
        }

        public IntegrationActionBlock(Action<IntegrationActionBlock, IntegratedDocument> action, int threadCount = 4)
            : base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            _action = ((act, x) =>
            {
                action(act, x);
                return x;
            });
        }

        public IntegrationActionBlock(string userId, Func<IntegrationActionBlock, IntegratedDocument, IntegratedDocument> action, int threadCount = 4)
            : base(capacity: 100000, procType: BlockType.Action, threadCount: threadCount)
        {
            this.UserId = userId;
            _action = action;
        }

        public IntegrationActionBlock(string userId, Action<IntegrationActionBlock, IntegratedDocument> action)
            : base(capacity: 100000, procType: BlockType.Action, threadCount : 4)
        {
            UserId = userId;
            _action = ((act, x) =>
            {
                action(act, x);
                return x;
            });
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
