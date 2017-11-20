using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing
{
    public class TokenCursor
    {
        public short Depth { get; set; }
        public short StartingDepth { get; private set; }
        public DslToken Token { get; private set; }

        public void SetToken(DslToken tok)
        {
            Token = tok;
        }

        public TokenCursor Clone()
        {
            var c = new TokenCursor();
            c.Depth = Depth;
            c.StartingDepth = c.StartingDepth;
            c.Token = Token;
            return c;
        }
    }
}