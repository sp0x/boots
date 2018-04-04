using Netlyt.Service.Lex.Data;

namespace Netlyt.Service.Lex
{
    public class DonutCodeContext
    {
        public DonutScript Script { get; private set; }

        public DonutCodeContext(DonutScript script)
        {
            this.Script = script;
        }
    }
}