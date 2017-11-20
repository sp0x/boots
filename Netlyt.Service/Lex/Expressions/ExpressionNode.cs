using System.Collections.Generic;
using Netlyt.Service.Lex.Parsing.Tokens;


namespace Netlyt.Service.Lex.Expressions
{
    public class ExpressionNode
        : IExpression
    {
        protected Stack<IExpression> Children { get; set; }
        public ExpressionNode Parent { get; set; }
        private DslToken Token { get; set; }
        public short Depth { get; private set; }

        public ExpressionNode()
        {
            Children = new Stack<IExpression>();
        }
        public ExpressionNode(DslToken token)
            : this()
        { 
            this.Token = token;
        }
        public void SetDepth(short d) { this.Depth = d; }

        public ExpressionNode AddChild(DslToken token)
        {
            var node = new ExpressionNode(token);
            Children.Push(node);
            return this;
        }

        public ExpressionNode AddChild(IExpression node)
        {
            Children.Push(node);
            return this;
        }

        public IEnumerable<IExpression> GetChildren() => Children;

        public int GetChildrenCount()
        {
            return Children.Count;
        }
    }
}