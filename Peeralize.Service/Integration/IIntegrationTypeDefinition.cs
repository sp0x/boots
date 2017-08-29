﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using MongoDB.Bson.Serialization.Attributes;
using nvoid.db.DB;
using nvoid.db.DB.MongoDB;
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
        string UserId { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string OriginType { get; }
        Dictionary<string, FieldDefinition> Fields { get; }

        IntegrationTypeExtras Extras { get; }
        bool Save();
        IIntegrationTypeDefinition SaveType(string userApiId);

        [BsonSerializer(typeof(LazySerializer))]
        Lazy<string> Id { get; set; }
         
    }
}