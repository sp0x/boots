using System;
using System.ComponentModel.DataAnnotations.Schema;
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
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public virtual Model Model { get; set; }

        public DonutScriptInfo()
        {

        }
        public DonutScriptInfo(IDonutScript dscript)
        {
            this.DonutScriptContent = dscript.ToString();
            this.AssemblyPath = dscript.AssemblyPath;
        }
        
        public IDonutScript GetScript()
        {
            try
            {
                var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
                var parser = new DonutSyntaxReader(tokenizer.Tokenize(DonutScriptContent));
                IDonutScript dscript = parser.ParseDonutScript(Model.GetRootIntegration());
                return dscript;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            
        }

        public override string ToString()
        {
            return DonutScriptContent;
        }
    }
}