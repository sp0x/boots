using System;
using Netlyt.Service.Ml;

namespace Netlyt.Service.Lex.Data
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
        public DonutScriptInfo(DonutScript dscript)
        {
            this.DonutScriptContent = dscript.ToString();
        }
        
        public DonutScript GetScript()
        {
            throw new NotImplementedException();
        }
    }
}