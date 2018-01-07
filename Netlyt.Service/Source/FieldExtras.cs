using System.Collections.Generic;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Source
{
    public class FieldExtras
    {
        public long Id { get; set; }
        public ICollection<FieldExtra> Extra { get; set; } 
        public bool Unique { get; set; }
        public bool Nullable { get; set; }
    }
}