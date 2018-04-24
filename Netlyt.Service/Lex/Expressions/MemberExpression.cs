using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Netlyt.Interfaces;

namespace Netlyt.Service.Lex.Expressions
{
    public class MemberExpression
        : Expression
    { 
        public IExpression ChildMember { get; set; }
        //public IExpression Parent { get; set; }
        //public TypeExpression Type { get; private set; }

        public MemberExpression(IExpression parent)
        {
            this.Parent = parent;
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
            return $"{Parent}{postfix}";
        }

        public string FullPath()
        {
            var buff = new StringBuilder();
            buff.Append(Parent);
            if (ChildMember == null) return buff.ToString();
            var element = ChildMember;
            buff.Append(".").Append(element);
            return buff.ToString();
        }
    }
}
