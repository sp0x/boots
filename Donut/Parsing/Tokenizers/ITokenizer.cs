using System.Collections.Generic;
using Donut.Parsing.Tokens;

namespace Donut.Parsing.Tokenizers
{
    public interface ITokenizer
    {
        IEnumerable<DslToken> Tokenize(string query);
    }
}
