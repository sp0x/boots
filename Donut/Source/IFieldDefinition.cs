using Netlyt.Interfaces;

namespace Donut.Source
{
    public interface IFieldDefinition
    {
        FieldDataEncoding DataEncoding { get; set; }
        FieldExtras Extras { get; set; }
        long Id { get; set; }
        Data.DataIntegration Integration { get; set; }
        long IntegrationId { get; set; }
        string Name { get; set; }
        string Type { get; set; }
    }
}