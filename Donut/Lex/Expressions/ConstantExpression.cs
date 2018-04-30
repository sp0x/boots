namespace Donut.Lex.Expressions
{
    public class NumberExpression
        : Expression
    {
        public int Value { get; set; } 

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    public class StringExpression
        : Expression
    {
        public string Value { get; set; } 
        public override string ToString()
        {
            return Value;
        }
    }
    public class FloatExpression
        : Expression
    {
        public float Value { get; set; } 
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
