using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using Model = Donut.Models.Model;

namespace Donut
{
    public class DonutScriptInfo
    {
        public long Id { get; set; }
        public string AssemblyPath { get; set; }
        public string DonutScriptContent { get; set; }
        public Model Model { get; set; }

        public DonutScriptInfo()
        {

        }
        public DonutScriptInfo(IDonutScript dscript)
        {
            this.DonutScriptContent = dscript.ToString();
        }
        
        public IDonutScript GetScript()
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(DonutScriptContent));
            IDonutScript dscript = parser.ParseDonutScript();
            return dscript;
        }
    }
}