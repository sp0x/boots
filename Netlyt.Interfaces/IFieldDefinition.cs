namespace Netlyt.Interfaces
{
    public interface IFieldDefinition
    {
        FieldDataEncoding DataEncoding { get; set; }
        IFieldExtras Extras { get; set; }
        long Id { get; set; }
        IIntegration Integration { get; set; }
        long IntegrationId { get; set; }
        string Name { get; set; }
        string Type { get; set; }
    }
}