using System.Collections.Generic;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration
{
    /// <summary>
    /// Describes a data type that will be used in an integration
    /// </summary>
    public interface IIntegration
    {
        long Id { get; set; }
        string Name { get; set; }
        int DataEncoding { get; set; }
        string APIKey { get; set; }
        string Collection { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string DataFormatType { get; }
        Dictionary<string, FieldDefinition> Fields { get; }

        IntegrationTypeExtras Extras { get; }
        IIntegration SaveType(string userApiId);

        IntegratedDocument CreateDocument<T>(T data);
    }
}