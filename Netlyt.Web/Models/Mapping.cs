using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nvoid.DB;
using Netlyt.Service.Integration;
using Netlyt.Web.Models.DataModels;

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
