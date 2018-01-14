using System.Collections.Generic;
using nvoid.Integration;
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
        ApiAuth APIKey { get; set; }
        string Collection { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string DataFormatType { get; }
        ICollection<FieldDefinition> Fields { get; }

        ICollection<IntegrationExtra> Extras { get; } 

        IntegratedDocument CreateDocument<T>(T data);

        string GetReducedCollectionName();
    }
}