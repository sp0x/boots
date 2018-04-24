using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    /// <summary>
    /// Describes a data type that will be used in an integration
    /// </summary>
    public interface IIntegration
    {
        long Id { get; set; }
        string Name { get; set; }
        int DataEncoding { get; set; }
        IApiAuth APIKey { get; set; }
        string Collection { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string DataFormatType { get; }
        ICollection<IFieldDefinition> Fields { get; }

        ICollection<IIntegrationExtra> Extras { get; } 

        IIntegratedDocument CreateDocument<T>(T data);

        string GetReducedCollectionName();
    }
}