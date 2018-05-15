using System;
using System.Collections.Generic;
using System.IO;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Donut
{
    public class ExpressionSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value.GetType();
            var valExp = value as IExpression;
            writer.WriteStartObject();
            var props = valExp.GetType().GetProperties();
            foreach (var prop in props)
            {
                var pval = prop.GetValue(valExp);
                writer.WritePropertyName(prop.Name);
                serializer.Serialize(writer, pval);
            }
            writer.WritePropertyName("_t");
            writer.WriteValue(type.FullName);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IExpression newInstance = Activator.CreateInstance(objectType) as IExpression;
            var json = JObject.Load(reader);
            foreach (var key in json.Properties())
            {
                var instanceProp = objectType.GetProperty(key.Name);
                if (instanceProp == null) continue;
                if (typeof(IExpression).IsAssignableFrom(instanceProp.PropertyType))
                {
                    var expPropertyType = key.Value.Type != JTokenType.Null ? Type.GetType(key.Value["_t"].ToString()) : null;
                    var propVal = serializer.Deserialize(key.Value.CreateReader(), expPropertyType);
                    instanceProp.SetValue(newInstance, propVal);
                }
                else if (instanceProp.PropertyType.IsGenericType &&
                         typeof(IExpression).IsAssignableFrom(instanceProp.PropertyType.GenericTypeArguments[0]))
                {
                    var propVal = serializer.Deserialize(key.Value.CreateReader(), instanceProp.PropertyType);
                    instanceProp.SetValue(newInstance, propVal);
                }
                else
                {
                    var propVal = serializer.Deserialize(key.Value.CreateReader());
                    instanceProp.SetValue(newInstance, propVal);
                }
                
            }
            return newInstance;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IExpression).IsAssignableFrom(objectType);
        }
    }
    public class DynamicExpressionSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var expSerializer = new JsonSerializer();
            expSerializer.Converters.Add(new ExpressionSerializer());
            if (objectType == typeof(NameExpression))
            {
                return expSerializer.Deserialize(reader, objectType);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IExpression).IsAssignableFrom(objectType);
        }
    }
    public class PropertyExpressionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jsonArray = new JArray();
            var expSerializer = new JsonSerializer();
            expSerializer.Converters.Add(new ExpressionSerializer());
            foreach (ParameterExpression par in value as List<ParameterExpression>)
            {
                JObject jpar = new JObject();
                jpar["Parent"] = par.Parent==null ? null : JObject.FromObject(par.Parent);
                var valWriter = new StringWriter();
                expSerializer.Serialize(valWriter, par.Value);
                var paramVal = JObject.Parse(valWriter.ToString());
                jpar["Value"] = paramVal;
                jpar["_vt"] = par.Value.GetType().FullName;
                jpar["_pt"] = par.Parent == null ? null : par.Parent.GetType().FullName;
                jsonArray.Add(jpar);
            }
            writer.WriteRawValue(jsonArray.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jobj = JArray.Load(reader);
            var outputList = new List<ParameterExpression>();
            var expSerializer = new JsonSerializer();
            expSerializer.Converters.Add(new ExpressionSerializer());
            foreach (var element in jobj)
            {
                var valType = Type.GetType(element["_vt"].ToString());
                var parentType = Type.GetType(element["_pt"].ToString());
                var valueRaw = element["Value"].ToString();

                var valReader = new JsonTextReader(new StringReader(valueRaw));
                var paramValue = expSerializer.Deserialize(valReader, valType) as IExpression;

                var parentRaw = element["Parent"];
                var parentValue = parentRaw.Type != JTokenType.Null ? element["Parent"].ToObject(parentType) as IExpression : null;
                var param = new ParameterExpression(parentValue, paramValue);
                outputList.Add(param);
            }
            return outputList;
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(List<ParameterExpression>)) return true;
            return false;
        }
    }
}