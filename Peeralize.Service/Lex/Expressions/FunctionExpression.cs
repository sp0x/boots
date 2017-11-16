using System;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Lex.Expressions;
using Peeralize.Service.Lex.Parsing;

namespace Peeralize.Service.Lex.Expressions
{
    public class FunctionExpression
        : IExpression
    {
        public string Name { get; set; }
        public List<ParameterExpression> Parameters { get; private set; }

        public FunctionExpression()
        {
            Parameters = new List<ParameterExpression>();
        }

        public IEnumerable<IExpression> GetChildren() => Parameters;

        public void AddParameter(ParameterExpression fnParameter)
        {
            Parameters.Add(fnParameter);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name);
            sb.Append("(");
            var iParam = 0;
            foreach (var param in Parameters)
            {
                var paramStr = param.ToString();
                sb.Append(paramStr);
                iParam++;
                if (iParam < Parameters.Count)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
