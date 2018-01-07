using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using nvoid.db.DB.MongoDB;

namespace Netlyt.Service.Source
{
    public class FieldDefinition
    {
        public long Id { get; set; }
        [BsonSerializer(typeof(StringSerializer))]
        public string Name { get; set; }

        [BsonSerializer(typeof(TypeSerializer))]
        public string Type { get; set; }
        
        public FieldExtras Extras { get; set; }
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