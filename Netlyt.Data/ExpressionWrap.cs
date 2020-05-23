namespace Netlyt.Data
{
    public class ExpressionWrap<TExpression>
    {
        public LogicOpType LogicOp { get; set; }
        public TExpression Expression { get; set; }
        public ExpressionWrap(TExpression exp)
        {
            Expression = exp;
        }

    }
}