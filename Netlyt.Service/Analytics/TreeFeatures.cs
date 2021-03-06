﻿using System;
using System.Collections.Generic;
using Donut;
using Donut.Features;
using Netlyt.Interfaces;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;

namespace Netlyt.Service.Analytics
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