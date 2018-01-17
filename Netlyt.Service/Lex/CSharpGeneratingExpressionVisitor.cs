using System.Collections.Generic;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    public class CSharpGeneratingExpressionVisitor
        : ExpressionVisitor
    {
        public Dictionary<VariableExpression, string> Variables { get; private set; }
        public CSharpGeneratingExpressionVisitor()
            : base()
        {
            Variables = new Dictionary<VariableExpression, string>();
        }

        protected override string VisitVariableExpression(VariableExpression exp)
        {
            return base.VisitVariableExpression(exp);
        }

        protected override string VisitNumberExpression(NumberExpression exp)
        {
            return base.VisitNumberExpression(exp);
        }

        protected override string VisitParameter(ParameterExpression parameterExpression)
        {
            return base.VisitParameter(parameterExpression);
        }

        protected override string VisitFunctionCall(CallExpression exp)
        {
            return base.VisitFunctionCall(exp);
        }

        protected override string VisitAssignment(AssignmentExpression exp)
        {
            return base.VisitAssignment(exp);
        }

        public override string VisitBinaryExpression(BinaryExpression exp)
        {
            return base.VisitBinaryExpression(exp);
        }

        protected override string VisitStringExpression(StringExpression exp)
        {
            return base.VisitStringExpression(exp);
        }

        protected override string VisitFloatExpression(FloatExpression exp)
        {
            return base.VisitFloatExpression(exp);
        }
    }
}