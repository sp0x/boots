using System.Collections.Generic;

namespace Donut.Source
{
    public interface IFieldExtras
    {
        ICollection<FieldExtra> Extra { get; set; }
        FieldDefinition Field { get; set; }
        long Id { get; set; }
        bool Nullable { get; set; }
        bool Unique { get; set; }
        bool IsFake { get; set; }
    }
}