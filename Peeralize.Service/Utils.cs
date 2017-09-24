using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver; 

namespace Peeralize.Service
{
    public static class Utils
    {

        /// <summary>
        /// Creates an update from the list of key-value pairs
        /// </summary>
        /// <typeparam name="TRecord"></typeparam>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static UpdateDefinition<TRecord> ToMongoUpdate<TRecord, Tval>(this IEnumerable<KeyValuePair<string, Tval>> pairs)
        {
            var updater = Builders<TRecord>.Update;
            var updates = new List<UpdateDefinition<TRecord>>();
            foreach (var pair in pairs)
            {
                var updateDef = updater.Set(pair.Key, pair.Value);
                updates.Add(updateDef);
            }
            return updater.Combine(updates);
        }
        public static BsonArray ToBsonArray(this IEnumerable<BsonValue> values)
        {
            return new BsonArray(values.ToArray());
        }
        /// <summary>
        /// Slugify's text so that it is URL compatible. IE: "Can make food" -> "Can-make-food".
        /// </summary>
        public static string Slugify(string txt)
        {
            var str = txt.Replace(" ", "-");
            return Regex.Replace(str, @"[^\w\.\-]+", "");
        }
    }
}
