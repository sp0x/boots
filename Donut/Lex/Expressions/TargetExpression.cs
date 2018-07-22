using System;
using System.Collections.Generic;
using System.Linq;

namespace Donut.Lex.Expressions
{
    public class TargetExpression : Expression
    {
        public IEnumerable<string> Attributes { get; set; }

        public TargetExpression()
        {

        }
        public TargetExpression(params string[] names)
        {
            Attributes = names;
        }

        public override string ToString()
        {
            var targetNames = string.Join(", ", Attributes.ToArray());
            return $"target {targetNames}";
        }
    }
}
