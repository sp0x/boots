using System;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing.Tokenizers
{
    public interface ITokenizer
    {
        IEnumerable<DslToken> Tokenize(string query);
    }
}
