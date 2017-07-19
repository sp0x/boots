using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using nvoid.db.DB.MongoDB;

namespace Peeralize.Service.Source
{
    public class FieldDefinition
    {
        public FieldDefinition()
        { 
        }
        public FieldDefinition(string fName, Type fType)
        {
            Name = fName;
            Type = fType;
        }

        [BsonSerializer(typeof(StringSerializer))]
        public string Name { get; set; }

        [BsonSerializer(typeof(TypeSerializer))]
        public Type Type { get; set; }

        public FieldExtras Extras { get; set; }



    }
}