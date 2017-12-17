﻿using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Expressions
{
    public class UnaryExpression 
        : Expression
    {
        public IExpression Operand { get; set; }
        public DslToken Token { get; set; }

        public UnaryExpression(DslToken token)
        {
            this.Token = token;
        }

        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression> {Operand};
        }
    }
}
