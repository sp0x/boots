using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Lex.Data
{
    class TokenPositionComparer
        : IEqualityComparer<TokenPosition>
    {
        public bool Equals(TokenPosition x, TokenPosition y)
        {
            return x.Line == y.Line && x.Position == y.Position;
        }

        public int GetHashCode(TokenPosition obj)
        {
            return obj.GetHashCode();
        }
    }
}
