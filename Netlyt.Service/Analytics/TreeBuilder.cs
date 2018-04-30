using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Blocks;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Models;

namespace Netlyt.Service.Analytics
{
    public class TreeBuilder 
        : BaseFlowBlock<IntegratedDocument, Tuple<BehaviourTree, IntegratedDocument>>
    {
        private readonly string _userIdKey; 
        public TreeBuilder(int batchSize, string userIdKey , int threadCount = 8)
            : base(capacity: batchSize, procType: BlockType.Transform, threadCount: threadCount)
        {
            _userIdKey = userIdKey;
//            var flowOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = batchSize };
//            var transformer = new TransformBlock<dynamic, IntegratedDocument>((dynamic x) => IntegratedDocument.Wrap(x) as Task<IntegratedDocument>, flowOptions);
//            transformer.LinkTo(batchDest, new DataflowLinkOptions { PropagateCompletion = true });
//            batchDest.LinkTo(builder.GetInputBlock(), new DataflowLinkOptions { PropagateCompletion = true });
        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="tree"></param>
//        /// <param name="daySessions"></param>
//        private void AddTreeSessions(BehaviourTree tree, IEnumerable<DomainUserSession> daySessions)
//        {
//            var pairs = daySessions.Select(x => new KeyValuePair<string,double>(x.Domain, x.Duration.TotalSeconds)).ToList();
//            tree.Build(pairs);
//        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intDoc"></param>
        /// <returns></returns>
        protected override Tuple<BehaviourTree, IntegratedDocument> OnBlockReceived(IntegratedDocument intDoc)
        {
            BsonArray days = intDoc["daily_sessions"] as BsonArray;
            var userTree = new BehaviourTree();
            Parallel.ForEach(days.ToArray(), day =>
            {
                var pairs = (day as BsonArray).Select(x =>
                {
                    string domain = x["Domain"].ToString();
                    var duration0 = TimeSpan.Parse(x["Duration"].ToString());
                    return new KeyValuePair<string, double>(domain, duration0.TotalSeconds);
                }).ToList();
                userTree.Build(pairs);
            });
            return new Tuple<BehaviourTree, IntegratedDocument>(userTree, intDoc);
        }

        public static BehaviourTree BuildFromItems(IEnumerable<BsonDocument> queryable)
        {
            var tree = new BehaviourTree();
            Parallel.ForEach(queryable, (user) =>
            {
                var items = from x in user["Document"]["Sessions"].AsBsonArray
                    select new KeyValuePair<string, double>(x["Domain"].ToString(),
                        TimeSpan.Parse(x["Duration"].ToString()).TotalSeconds);
                tree.Build(items.ToList());
            });
            return tree;
        } 
    }
}
