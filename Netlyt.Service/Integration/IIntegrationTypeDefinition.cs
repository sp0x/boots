using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata;
using MongoDB.Bson.Serialization.Attributes;
using nvoid.db.DB;
using nvoid.db.DB.MongoDB;
using Netlyt.Service.Source;
using FieldDefinition = Netlyt.Service.Source.FieldDefinition;

namespace Netlyt.Service.Integration
{
    /// <summary>
    /// Describes a data type that will be used in an integration
    /// </summary>
    public interface IIntegrationTypeDefinition
    {
        string Name { get; } 
        int CodePage { get; }
        string UserId { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string OriginType { get; }
        Dictionary<string, FieldDefinition> Fields { get; }

        IntegrationTypeExtras Extras { get; }
        bool Save();
        IIntegrationTypeDefinition SaveType(string userApiId);

        [BsonSerializer(typeof(LazyStringSerializer))]
        Lazy<string> Id { get; set; }

        IntegratedDocument Wrap<T>(T data); 
    }
}