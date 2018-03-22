using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    public class DonutFunctionParser
    {
        private static Dictionary<string, DonutFunction> Functions { get; set; }
        static DonutFunctionParser()
        {
            Functions = new Dictionary<string, DonutFunction>();
            Functions["sum"] = new DonutFunction("sum")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$sum", "${0}" } }).ToString()
            };
            Functions["max"] = new DonutFunction("max")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$max", "${0}" } }).ToString()
            };
            Functions["min"] = new DonutFunction("min")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$min", "${0}" } }).ToString()
            };
            Functions["num_unique"] = new DonutFunction("num_unique")
            { IsAggregate = true };
            Functions["std"] = new DonutFunction("std")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$stdDevSamp", "${0}" } }).ToString()
            };
            Functions["mean"] = new DonutFunction("mean")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$avg", "${0}" } }).ToString()
            };
            Functions["avg"] = new DonutFunction("avg")
            {
                Type = DonutFunctionType.Group,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$avg", "${0}" } }).ToString()
            };
            Functions["num_unique"] = new DonutFunction("num_unique")
            {
                Type = DonutFunctionType.GroupKey,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "{0}", "{1}"} }).ToString()
            };
            Functions["skew"] = new DonutFunction("skew")
            { IsAggregate = true };
            Functions["day"] = new DonutFunction("day")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfMonth", "${0}" } }).ToString()
            };
            Functions["month"] = new DonutFunction("month")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$month", "${0}" } }).ToString()
            };
            Functions["year"] = new DonutFunction("year")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$year", "${0}" } }).ToString()
            };
            Functions["weekday"] = new DonutFunction("weekday")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfWeek", "${0}" } }).ToString()
            };
            Functions["mode"] = new DonutFunction("mode")
            { IsAggregate = false };
            //Functions["time"] = "(function(timeElem){ return timeElem.getTime() })";
        }

        private static string GetAggtegateBody(BsonDocument filter)
        {
            throw new NotImplementedException();
        }

        private static string GetDayFn()
        {
            return "Utils.GetDay";
        }
        private static string GetWeekdayFn()
        {
            return "Utils.GetWeekDay";
        }
        private static string GetMonthFn()
        {
            return "Utils.GetMonth";
        }
        private static string GetYearFn()
        {
            return "Utils.GetYear";
        }
        public DonutFunction Resolve(string function, List<ParameterExpression> expParameters)
        {
            DonutFunction output = null;
            var lower = function.ToLower();
            if (Functions.ContainsKey(lower))
            {
                output = Functions[lower].Clone();
                output.Parameters = expParameters;
            }
            else
            {
                throw new Exception($"Unsupported js function: {function}");
            }
            return output;
        }

        public bool IsAggregate(CallExpression callExpression)
        {
            var fKey = callExpression.Name.ToLower();
            if (Functions.ContainsKey(fKey))
            {
                var fn = Functions[fKey];
                return fn.IsAggregate;
            }
            else
            {
                return false;
            }
        }

        public DonutFunctionType GetFunctionType(CallExpression callExpression)
        {
            var fKey = callExpression.Name.ToLower();
            if (Functions.ContainsKey(fKey))
            {
                var fn = Functions[fKey];
                return fn.Type;
            }
            else
            {
                return DonutFunctionType.Standard;
            }
        }
    }
}