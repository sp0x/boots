using Donut.Lex.Generators;
using Netlyt.Interfaces;

namespace Donut
{
    public class AggregateKey : IAggregateKey
    {
        public string Name { get; set; }
        public IDonutFunction Operation { get; set; }
        public string Arguments { get; set; }

        public AggregateKey(string name, string fn, string argumments)
        {
            this.Name = name;
            this.Arguments = argumments;
            if (fn != null)
            {
                var fns = (new DonutFunctions());
                Operation = fns.GetFunction(fn);
            }
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