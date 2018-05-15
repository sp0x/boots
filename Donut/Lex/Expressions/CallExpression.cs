using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class CallExpression
        : Expression
    {
        public string Name { get; set; }
        public List<ParameterExpression> Parameters { get; private set; }

        public CallExpression()
        {
            Parameters = new List<ParameterExpression>();
        }

        public override IEnumerable<IExpression> GetChildren() => Parameters.Cast<IExpression>();

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
