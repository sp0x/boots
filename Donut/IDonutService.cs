using System;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Lex.Data;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutService
    {
        Task<IHarvesterResult> RunExtraction(DonutScript script, DataIntegration integration,
            IServiceProvider serviceProvider);
        Task<string> ToPythonModule(DonutScriptInfo donut);
    }
}