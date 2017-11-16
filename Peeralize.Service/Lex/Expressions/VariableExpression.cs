using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Lex.Expressions
{
    public class VariableExpression
        : IExpression
    {
        public string Name { get; private set; }

        public VariableExpression(string name)
        {
            this.Name = name;
        }

        public IEnumerable<IExpression> GetChildren()
        {
            return new List<ExpressionNode>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
