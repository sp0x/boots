using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing.Tokenizers
{
    public interface ITokenizer
    {
        IEnumerable<DslToken> Tokenize(string query);
    }
}
