using System;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Netlyt.Interfaces;

namespace Donut.Source
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
        [ForeignKey("Extras")]
        public long ExtrasId { get; set; }
        public FieldExtras Extras { get; set; }
        public FieldDataEncoding DataEncoding { get; set; }
        [ForeignKey("Integration")]
        public long IntegrationId { get; set; }
        public Data.DataIntegration Integration { get; set; }
        public FieldDefinition()
        {
        }

        public FieldDefinition(string fName, Type fType)
        {
            Name = fName;
            Type = fType.FullName;
        }
        public FieldDefinition(string fName, string fType)
        {
            Name = fName;
            Type = fType;
        }

    }
}