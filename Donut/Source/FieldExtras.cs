using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Netlyt.Interfaces;

//using Netlyt.Service.Integration;

namespace Donut.Source
{
    public class FieldExtras : IFieldExtras
    {
        public long Id { get; set; }
        public ICollection<FieldExtra> Extra { get; set; } 
        public bool Unique { get; set; }
        public bool Nullable { get; set; }
        [ForeignKey("Field")]
        public long FieldId { get; set; }
        public FieldDefinition Field { get; set; }
        public bool IsFake { get; set; }

        public FieldExtras()
        {
            Extra = new HashSet<FieldExtra>();
        }
    }
}