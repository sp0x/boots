﻿using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    public class BlockExpression
        : IExpression
    {
        public List<IExpression> Children { get; set; }

        public BlockExpression()
        {
            Children = new List<IExpression>();
        }

        public IEnumerable<IExpression> GetChildren()
        {
            return Children;
        }
    }
}