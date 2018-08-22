using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Lex.Data;
using Donut.Models;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut
{
    public interface IDonutService
    {
        Task<IHarvesterResult> RunExtraction(DonutScript script, DataIntegration integration,
            IServiceProvider serviceProvider);
        Task<string> ToPythonModule(DonutScriptInfo donut);
        string GetSnippet(User user, TrainingTask trainingTask, string language);
        Dictionary<string,string> GetSnippets(User user, TrainingTask trainingTask);
        Task<Tuple<string, DonutScriptInfo>> GeneratePythonModule(long id, User user);
    }
}