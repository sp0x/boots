using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Peeralize.Middleware
{
    public static class Extensions
    {
        public static JToken ReadBodyAsJson(this HttpRequest request)
        {
            var jsonReader = new JsonTextReader(new StreamReader(request.Body));
            var serializer = new JsonSerializer();
            JToken bodyJson = serializer.Deserialize<JToken>(jsonReader);
            return bodyJson;
        }
        public static JToken ReadBodyAsPrefixedJson(this HttpRequest request, string prefix)
        {
            var jsonReader = new JsonTextReader(new StreamReader(request.Body));
            var serializer = new JsonSerializer();
            JToken bodyJson = serializer.Deserialize<JToken>(jsonReader);
            return bodyJson;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static JToken PrefixKeys(this JToken content, string prefix, bool deep = false)
        {
            JProperty prop = content as JProperty;
            //Rename a property
            if (prop != null)
            {
                return new JProperty($"{prefix}{prop.Name}", PrefixKeys(prop.Value, prefix));
            }
            JArray arr = content as JArray;
            if (arr != null)
            {
                var cont = arr.Select(el => PrefixKeys(el, prefix));
                return new JArray(cont);
            }
            JObject o = content as JObject;
            if (o != null)
            {
                var cont = o.Properties().Select(el => PrefixKeys(el, prefix));
                return new JObject(cont);
            }
            return content;
        }
    }
}
