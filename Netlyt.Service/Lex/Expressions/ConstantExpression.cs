using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public class NumberExpression
        : IExpression
    {
        public int Value { get; set; }
        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    public class StringExpression
        : IExpression
    {
        public string Value { get; set; }
        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>();
        }
        public override string ToString()
        {
            return Value;
        }
    }
    public class FloatExpression
        : IExpression
    {
        public float Value { get; set; }
        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>();
        }
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
