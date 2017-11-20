using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public class MemberExpression
        : IExpression
    {
        public string Name { get; private set; }
        public IExpression SubElement { get; set; }
        //public TypeExpression Type { get; private set; }

        public MemberExpression(string name)
        {
            this.Name = name;
        }
        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>() { SubElement };
        }

        public override string ToString()
        {
            var postfix = SubElement==null ? "" : SubElement.ToString();
            if (!string.IsNullOrEmpty(postfix)) postfix = $".{postfix}";
            else postfix = "";
            return $"{Name}{postfix}";
        }
    }
}
