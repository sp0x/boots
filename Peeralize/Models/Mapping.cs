using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nvoid.DB;
using Peeralize.Service.Integration;

namespace Peeralize.Models
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
