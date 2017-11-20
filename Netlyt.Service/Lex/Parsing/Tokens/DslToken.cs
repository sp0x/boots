namespace Netlyt.Service.Lex.Parsing.Tokens
{
    public class DslToken
    {

        public uint Line { get; private set; }
        public uint Position { get; set; }
        public TokenType TokenType { get; set; }
        public string Value { get; set; }

        public DslToken(TokenType tokenType)
        {
            TokenType = tokenType;
            Value = string.Empty;
        }

        public DslToken(TokenType tokenType, string value, uint line)
        {
            TokenType = tokenType;
            Value = value;
            Line = line;
        }

        public DslToken Clone()
        {
            return new DslToken(TokenType, Value, Line)
            {
                Position = this.Position
            };
        }

        public override string ToString()
        {
            return $"[{TokenType}]{Value}";
        }
    }
}