using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public class MemberExpression
        : Expression
    {
        public string Name { get; private set; }
        public MemberExpression ChildMember { get; set; }
        //public TypeExpression Type { get; private set; }

        public MemberExpression(string name)
        {
            this.Name = name;
        }
        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>() { ChildMember };
        }

        public override string ToString()
        {
            var postfix = ChildMember==null ? "" : ChildMember.ToString();
            if (!string.IsNullOrEmpty(postfix)) postfix = $".{postfix}";
            else postfix = "";
            return $"{Name}{postfix}";
        }

        public string FullPath()
        {
            var buff = new StringBuilder();
            buff.Append(Name);
            var element = ChildMember;
            while (element != null)
            {
                buff.Append(".").Append(element.Name);
                element = element.ChildMember;
            }
            return buff.ToString();
        }
    }
}
