using System;
using System.Linq;
using nvoid.Crypto;
using Netlyt.Service.Lex;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Donut
{
    public class DsTime : DonutFunction, IDonutTemplateFunction
    {
        public DsTime(string nm) : base(nm)
        {
        }

        public string GetTemplate(CallExpression exp, DonutCodeContext ctx)
        {
            var callParam = exp.Parameters.FirstOrDefault();
            VariableExpression dsName = callParam.Value as VariableExpression;
            var targetExpression = ctx.Script.GetDatasetMember(dsName.Name);
            if (targetExpression != null)
            {
                return $"\"${targetExpression.Integration.DataTimestampColumn}\"";
            }
            else
            {
                throw new Exception("Integration not found");
            }
        }

        public override int GetHashCode()
        {
            var content = GetValue();
            return (int)HashAlgos.Adler32(content);
        }
    }
}