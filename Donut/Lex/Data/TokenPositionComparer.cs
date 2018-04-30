using System.Collections.Generic;

namespace Donut.Lex.Data
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
