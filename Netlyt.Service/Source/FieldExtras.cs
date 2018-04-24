using System.Collections.Generic;
using Netlyt.Interfaces;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Source
{
    public class FieldExtras : IFieldExtras
    {
        public long Id { get; set; }
        public ICollection<IFieldExtra> Extra { get; set; } 
        public bool Unique { get; set; }
        public bool Nullable { get; set; }
        public IFieldDefinition Field { get; set; }

        public FieldExtras()
        {
            Extra = new HashSet<IFieldExtra>();
        }
    }
}