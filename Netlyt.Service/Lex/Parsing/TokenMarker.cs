using System.Collections.Generic;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing
{
    public class TokenMarker
    {
        public short Depth { get; set; } 
        public DslToken Token { get; private set; }
        public IEnumerable<DslToken> NextTokens { get; private set; }

        public void SetToken(DslToken tok, IEnumerable<DslToken> nextTokens = null)
        {
            Token = tok;
            if(nextTokens!=null) this.NextTokens = nextTokens;
        }

        public TokenMarker Clone()
        {
            var c = new TokenMarker();
            c.Depth = Depth; 
            c.Token = Token;
            return c;
        }

        public void SetNextTokens(IEnumerable<DslToken> nextToks)
        {
            NextTokens = nextToks;
        }
    }
}