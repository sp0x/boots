using System;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Expressions
{
    public class UnaryExpression 
        : IExpression
    {
        public IExpression Operand { get; set; }
        public DslToken Token { get; set; }

        public UnaryExpression(DslToken token)
        {
            this.Token = token;
        }

        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression> {Operand};
        }
    }
}
