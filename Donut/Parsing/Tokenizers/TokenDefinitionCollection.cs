using System.Collections.Generic;

namespace Donut.Parsing.Tokenizers
{
    public abstract class TokenDefinitionCollection
    {
        protected List<TokenDefinition> TokenDefinitions { get; set; }

        public TokenDefinitionCollection()
        {
            TokenDefinitions = new List<TokenDefinition>();
        }

        public List<TokenDefinition> GetAll()
        {
            return TokenDefinitions;
        }
    }
}