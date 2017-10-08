using System;
using System.Collections.Generic;
using Peeralize.Service.Integration;
using Peeralize.Service.Models;

namespace Peeralize.Service.Analytics
{
    public class TreeFeatures : FeaturesWrapper<Tuple<BehaviourTree, IntegratedDocument>>
    {
        public TreeFeatures()
        {
            
        }
        public TreeFeatures(Tuple<BehaviourTree, IntegratedDocument> doc, 
            IEnumerable<KeyValuePair<string,object>> features)
            : base(doc, features)
        { 

        }
    }
}