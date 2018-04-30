namespace Donut.Lex.Expressions
{
    public class TargetExpression : Expression
    {
        public string Attribute { get; set; }

        public TargetExpression(string name)
        {
            Attribute = name;
        }

        public override string ToString()
        {
            return $"target {Attribute}";
        }
    }
}
