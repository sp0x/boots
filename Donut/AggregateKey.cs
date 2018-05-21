using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Donut.Data;
using Donut.Lex.Generators;
using MongoDB.Bson;
using Netlyt.Interfaces;

namespace Donut
{
    /// <summary>
    /// The key that's used to aggregate data.
    /// </summary>
    public class AggregateKey : IAggregateKey
    {
        public long Id { get; set; }
        public string Name { get; set; }
        [ForeignKey("Operation")]
        public long? OperationId { get; set; }
        public virtual DonutFunction Operation { get; set; }
        public string Arguments { get; set; }
        public AggregateKey() { }
        public AggregateKey(string name, string fn, string argumments)
        {
            this.Name = name;
            this.Arguments = argumments;
            if (!String.IsNullOrEmpty(fn))
            {
                var fns = (new DonutFunctions());
                Operation = DonutFunction.Wrap(fns.GetFunction(fn));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public object GetValue(BsonDocument obj)
        {
            if (Operation?.Eval == null) return null;
            try
            {
                var bsonDocument = obj[Arguments];
                return Operation.EvalValue(bsonDocument);
            }catch(Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="doc"></param>
        /// <returns></returns>
        public KeyValuePair<string, object> GetKeyValuePair(BsonDocument doc)
        {
            var val = GetValue(doc);
            var kvp = new KeyValuePair<string, object>(Arguments, val);
            return kvp;
        }

        public override string ToString()
        {
            if (Operation != null)
            {
                var skey = "new BsonDocument { { \"" + Name + "\", BsonDocument.Parse(";
                var opContent = Operation.GetValue();
                var targetKey = AggregateStage.FormatDonutFnAggregateParameter(opContent, null, opContent, 0, Arguments);
                skey += "\"" + targetKey.Replace("\"","\\\"") + "\"";
                //"{ \"$hour\", \"$" + tsKey + "\"}" +
                skey += ") }}";
                return skey;
            }
            else
            {
                var skey = "new BsonDocument{ { \"" + Name + "\", \"" + Arguments + "\"} }";
                return skey;
            }
        }

    }
}