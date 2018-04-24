using System;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Source
{

    public class FieldDefinition : IFieldDefinition
    {
        public long Id { get; set; }
        [BsonSerializer(typeof(StringSerializer))]
        public string Name { get; set; }
        /// <summary>
        /// The clr type of the field
        /// </summary>
        [BsonSerializer(typeof(TypeSerializer))]
        public string Type { get; set; }
        
        public IFieldExtras Extras { get; set; }
        public FieldDataEncoding DataEncoding { get; set; }
        [ForeignKey("Integration")]
        public long IntegrationId { get; set; }
        public IIntegration Integration { get; set; }
        public FieldDefinition()
        {
        }

        public FieldDefinition(string fName, Type fType)
        {
            Name = fName;
            Type = fType.FullName;
        }

    }
}