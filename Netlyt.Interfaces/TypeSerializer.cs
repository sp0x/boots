using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Netlyt.Interfaces
{
    
    public class LazySerializer<T> : IBsonSerializer<Lazy<T>>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Lazy<T> value)
        {
            Lazy<T> dt = (Lazy<T>)value;
            if (dt == null)
            {
                context.Writer.WriteNull();
            }
            else
            {
                var dtString = value.Value.ToBsonDocument().ToString();
                context.Writer.WriteString(dtString);
            }
        }

        public Lazy<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var value = context.Reader.ReadString();
            return new Lazy<T>(()=> BsonSerializer.Deserialize<T>(value)); 
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Lazy<T> dt = (Lazy<T>)value;
            if (dt == null)
            {
                context.Writer.WriteNull();
            }
            else
            {
                var dtString = dt.Value.ToBsonDocument().ToString();
                context.Writer.WriteString(dtString);
            }  
        }

        public Type ValueType => typeof(Lazy<T>);
    }
    public class LazyStringSerializer : IBsonSerializer<Lazy<string>>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Lazy<string> value)
        {
            Lazy<string> dt = (Lazy<string>)value;
            context.Writer.WriteString(dt == null ? String.Empty : dt.Value);
        }

        public Lazy<string> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            string rawValue = context.Reader.ReadString(); 
            return new Lazy<string>(rawValue.ToString);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Lazy<string> dt = (Lazy<string>)value;
            context.Writer.WriteString(dt == null ? String.Empty : dt.Value);
        }

        public Type ValueType => typeof(Lazy<string>);
    }

    /// <summary>
    /// Serialize responsible for types
    /// </summary>
    public class TypeSerializer : IBsonSerializer
    {
        private static readonly IBsonSerializer _default = new BsonClassMapSerializer<Type>(BsonClassMap.LookupClassMap(typeof(Type)));
        public TypeSerializer()
        {
        }
        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            string typeName = context.Reader.ReadString();
            var type = string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
            return type;
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Type dt = (Type)value; 
            context.Writer.WriteString(dt==null ? String.Empty : dt.FullName);
        }

        public Type ValueType => typeof(Type);
    }
}