﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nvoid.DB;
using Netlyt.Service.Integration;

namespace Netlyt.Models
{
    [EntityMapping(Name = "Peeralize generic mapping")]
    public class Mapping : AssemblyEntityMap
    {
        public override IEnumerable<Type> Types()
        {
            return new List<Type>()
            {
                typeof(IntegratedDocument)
            };
        }
    }
}
