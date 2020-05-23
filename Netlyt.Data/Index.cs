using System.Collections.Generic;

namespace Netlyt.Data
{
    public class Index
    {
        public bool Unique { get; private set; }
        public string Name { get; private set; }
        public List<IndexKey> Keys { get; set; }
        public Index(string name, List<IndexKey> keys, bool unique)
        {
            this.Name = name;
            this.Unique = unique;
            this.Keys = keys;
        }
    }
}