using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IFieldExtras
    {
        ICollection<IFieldExtra> Extra { get; set; }
        IFieldDefinition Field { get; set; }
        long Id { get; set; }
        bool Nullable { get; set; }
        bool Unique { get; set; }
    }
}