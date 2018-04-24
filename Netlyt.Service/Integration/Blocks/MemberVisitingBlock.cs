using System;
using System.Collections.Generic;
using MongoDB.Bson;
using nvoid.exec.Blocks;
using Netlyt.Interfaces;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// Gets a document's member and iterates through the member's elements, performing the desired action.
    /// </summary>
    public class MemberVisitingBlock<T> : BaseFlowBlock<T, T>
    where T : class, IIntegratedDocument
    {
        /// <summary>
        /// 
        /// </summary>
        private Action<T> _action; 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyResolver">Key generation method. Only unique keys are supported.</param>
        /// <param name="action">The action to perform on each child fetched from childSelector</param>
        /// <param name="childSelector">Children fetcher.</param>
        /// <param name="threadCount"></param>
        public MemberVisitingBlock( 
            Action<T> action, 
            int threadCount = 4,
            int capacity = 1000) : base(capacity: capacity, procType: BlockType.Action, threadCount: threadCount)
        { 
            _action = action; 
        }
        protected override T OnBlockReceived(T intDoc)
        {
            _action(intDoc);
            return intDoc;
        }


        protected override IEnumerable<T> GetCollectedItems()
        {
            return null;
        }
    }
}