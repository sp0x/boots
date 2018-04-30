using Donut.Lex.Data;

namespace Donut
{
    public class DonutCodeContext
    {
        public IDonutScript Script { get; private set; }

        public DonutCodeContext(IDonutScript script)
        {
            this.Script = script;
        }
    }
}