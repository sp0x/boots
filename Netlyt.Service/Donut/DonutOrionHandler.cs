using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netlyt.Service.Data;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Ml;
using Netlyt.Service.Orion;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Donut
{
    public class DonutOrionHandler
    {
        private OrionContext _orion;
        private ManagementDbContext _db;
        private CompilerService _compiler;

        public DonutOrionHandler(IFactory<ManagementDbContext> contextFactory, OrionContext orion, CompilerService compilerService)
        {
            _db = contextFactory.Create();
            _orion = orion;
#pragma warning disable 4014
            _orion.FeaturesGenerated += (x)=> _orion_FeaturesGenerated(x);
#pragma warning restore 4014
            _compiler = compilerService;
        }

        /// <summary>
        /// Handle the features that orion generated, and assign them.
        /// </summary>
        /// <param name="featureResult"></param>
        /// <returns></returns>
        private async Task _orion_FeaturesGenerated(JObject featureResult)
        {
            var fscript = featureResult["result"]?.ToString();
            if (string.IsNullOrEmpty(fscript)) return;
            var features = fscript.Split('\n');
            var parameters = featureResult["params"];
            long modelId = long.Parse(parameters["model_id"].ToString()); 
            Model model = _db.Models.Include(x=>x.DataIntegrations).FirstOrDefault(x => x.Id == modelId);
            if (model == null) return;
            var featureBodies = features.Select(x => x.ToString()).ToArray();
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, featureBodies);
            foreach (var integration in model.DataIntegrations)
            {
                var ign = _db.Integrations.FirstOrDefault(x => x.Id == integration.IntegrationId);
                dscript.AddIntegrations(ign);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName , out donutType, out donutContextType, out donutFEmitterType);
            model.DonutScript = new DonutScriptInfo(dscript);
            model.DonutScript.AssemblyPath = assembly.Location;
            model.DonutScript.Model = model;
            _db.SaveChanges();
            //We got the model
            //Start training
            var trainingResult = await TrainGeneratedFeatures(model,dscript,  assembly, donutType, donutContextType, donutFEmitterType);
            trainingResult = trainingResult;
        }

        private async Task<object> TrainGeneratedFeatures(Model model,
            DonutScript ds,
            Assembly assembly,
            Type donutType,
            Type donutContextType,
            Type donutFEmitterType)
        {
            var query = OrionQuery.Factory.CreateTrainQuery(model);
            var m_id = await _orion.Query(query);
            return null;
        }
    }
}