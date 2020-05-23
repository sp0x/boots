using System.Collections.Generic;

namespace Netlyt.Data
{
    public class EntitiesConfiguration// : EntitiesEntryCollection
    { 
        public string Base { get; set; }
        public string Assembly { get; set; }
        public IList<EntityConfiguration> Entities { get; set; }
    }
}