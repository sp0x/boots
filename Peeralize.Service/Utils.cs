using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Peeralize.Service
{
    public static class Utils
    {


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
