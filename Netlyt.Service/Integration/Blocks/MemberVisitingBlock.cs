using System;
using System.Collections.Generic;
using MongoDB.Bson;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// Gets a document's array member and iterates through the member's elements, performing the desired action.
    /// </summary>
    public class MemberVisitingBlock
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        /// <summary>
        /// 
        /// </summary>
        private Action<IntegratedDocument> _action; 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyResolver">Key generation method. Only unique keys are supported.</param>
        /// <param name="action">The action to perform on each child fetched from childSelector</param>
        /// <param name="childSelector">Children fetcher.</param>
        /// <param name="threadCount"></param>
        public MemberVisitingBlock( 
            Action<IntegratedDocument> action, 
            int threadCount = 4,
            int capacity = 1000) : base(capacity: capacity, procType: BlockType.Action, threadCount: threadCount)
        { 
            _action = action; 
        }
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            _action(intDoc);
            return intDoc;
        }


        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return null;
        }
    }
}