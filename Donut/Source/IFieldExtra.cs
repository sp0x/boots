using Netlyt.Interfaces;

namespace Donut.Source
{
    public interface IFieldExtra
    {
        FieldDefinition Field { get; set; }
        long Id { get; set; }
        string Key { get; set; }
        FieldExtraType Type { get; set; }
        string Value { get; set; }
    }
}