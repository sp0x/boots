using Donut.Parsing.Tokens;

namespace Donut.Parsing.Tokenizers
{
    public class TokenMatch
    {
        public TokenType TokenType { get; set; }
        public string Value { get; set; }
        public int StartIndex { get; set; }
        public uint Line { get; set; }
        public int EndIndex { get; set; }
        public int Precedence { get; set; }
        public override string ToString()
        {
            return Value;
            //return base.ToString();
        }
    }
}