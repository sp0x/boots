using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing.Tokenizers
{
    public class TokenMatch
    {
        public TokenType TokenType { get; set; }
        public string Value { get; set; }
        public int StartIndex { get; set; }
        public uint Line { get; set; }
        public int EndIndex { get; set; }
        public int Precedence { get; set; }
    }
}