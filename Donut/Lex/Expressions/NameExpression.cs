using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    /// <summary>
    /// A name expression
    /// </summary>
    public class NameExpression : Expression
    {

        public string Name { get; private set; }
        public MemberExpression Member { get; set; }

        public NameExpression()
        {

        }
        public NameExpression(string name)
        {
            this.Name = name;
        }

        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>() { Member };
        }

        public override string ToString()
        {
            var buff = new StringBuilder(Name);
            if (Member != null)
            {
                string postfix = Member.FullPath();
                buff.Append(".").Append(postfix);
            }
            return buff.ToString();
        }

        
    }
}
