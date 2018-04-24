namespace Netlyt.Interfaces
{
    public interface IFieldExtra
    {
        IFieldDefinition Field { get; set; }
        long Id { get; set; }
        string Key { get; set; }
        FieldExtraType Type { get; set; }
        string Value { get; set; }
    }
}