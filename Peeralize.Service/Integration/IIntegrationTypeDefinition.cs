using System.Collections.Generic;
using System.Reflection.Metadata;
using nvoid.db.DB;
using Peeralize.Service.Source;
using FieldDefinition = Peeralize.Service.Source.FieldDefinition;

namespace Peeralize.Service.Integration
{
    /// <summary>
    /// Describes a data type that will be used in an integration
    /// </summary>
    public interface IIntegrationTypeDefinition
    {
        string Name { get; } 
        int CodePage { get; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string OriginType { get; }
        Dictionary<string, FieldDefinition> Fields { get; }

        IntegrationTypeExtras Extras { get; }
        bool Save();
        string Id { get; set; }
         
    }
}