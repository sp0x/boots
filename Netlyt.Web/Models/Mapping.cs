using System;
using System.Collections.Generic; 
using nvoid.DB;
using Netlyt.Service;
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;

namespace Netlyt.Web.Models
{
    [EntityMapping(Name = "Netlyt generic mapping")]
    public class Mapping : AssemblyEntityMap
    {
        public override IEnumerable<Type> Types()
        {
            return new List<Type>()
            {
                typeof(IntegratedDocument),
                typeof(Model),
                typeof(Rule),
                typeof(User),
            };
        }
    }
}
