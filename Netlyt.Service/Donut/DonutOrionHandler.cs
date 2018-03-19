using System;
using System.Linq;
using System.Threading.Tasks;
using Netlyt.Service.Data;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Orion;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Donut
{
    public class DonutOrionHandler
    {
        private OrionContext _orion;
        private ManagementDbContext _db;
        private CompilerService _compiler;

        public DonutOrionHandler(OrionContext orionContext, ManagementDbContext db, CompilerService compiler)
        {
            _db = db;
            _orion = orionContext;
            _orion.FeaturesGenerated += (x)=> _orion_FeaturesGenerated(x);
            _compiler = compiler;
        }

        private async Task _orion_FeaturesGenerated(Newtonsoft.Json.Linq.JObject featureResult)
        {
            JArray features = featureResult["result"] as JArray;
            string taskId = featureResult["task_id"].ToString();
            if (string.IsNullOrEmpty(taskId)) return;
            var task = _db.FeatureGenerationTasks.FirstOrDefault(x => x.OrionTaskId == taskId);
            if (task == null) return;
            task.Status = Models.FeatureGenerationTaskStatus.Done;
            var model = task.Model;
            var featureBodies = features.Select(x => x.ToString()).ToArray();
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, featureBodies);
            foreach (var integration in model.DataIntegrations)
            {
                dscript.AddIntegrations(integration.Integration.Collection);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName , out donutType, out donutContextType, out donutFEmitterType);
            model.DonutScript = new DonutScriptInfo(dscript);
            model.DonutScript.AssemblyPath = assembly.Location;
            model.DonutScript.Model = model;
            _db.SaveChanges();
        }
    }
}