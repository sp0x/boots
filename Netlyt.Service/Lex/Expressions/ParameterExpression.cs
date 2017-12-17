﻿using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    public class ParameterExpression
        : Expression
    {
        public IExpression Value { get; private set; }

        public ParameterExpression(IExpression value)
        {
            this.Value = value;
        }
        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>() {Value};
        }

        public override string ToString()
        {
            return Value?.ToString();
        }
    }
}
