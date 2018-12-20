using System;
using System.ComponentModel.DataAnnotations.Schema;
using Donut.Data;
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
        public long? ExtrasId { get; set; }
        public virtual FieldExtras Extras { get; set; }
        public string DescriptionJson { get; set; }
        public string DataType { get; set; }

        public FieldDataEncoding DataEncoding { get; set; }
        [ForeignKey("Integration")]
        public long IntegrationId { get; set; }
        public Data.DataIntegration Integration { get; set; }
        public string TargetType { get; set; }
        public string Language { get; set; }

        public FieldDefinition()
        {
        }

        public FieldDefinition(string fName, Type fType)
        {
            Name = CleanupName(fName);
            Type = fType.FullName;
        }

        private string CleanupName(string fName)
        {
            return Cleanup.CleanupFieldName(fName);
        }

        public FieldDefinition(string fName, string fType)
        {
            Name = fName;
            Type = fType;
        }

    }
}