using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing
{
    public class TokenCursor
    {
        public short Depth { get; set; } 
        public DslToken Token { get; private set; }

        public void SetToken(DslToken tok)
        {
            Token = tok;
        }

        public TokenCursor Clone()
        {
            var c = new TokenCursor();
            c.Depth = Depth; 
            c.Token = Token;
            return c;
        }
    }
}