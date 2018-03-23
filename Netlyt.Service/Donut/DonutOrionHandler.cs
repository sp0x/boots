using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using Netlyt.Service.Data;
using Netlyt.Service.FeatureGeneration;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Ml;
using Netlyt.Service.Orion;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Donut
{
    public class DonutOrionHandler : IDisposable
    {
        private OrionContext _orion;
        private ManagementDbContext _db;
        private CompilerService _compiler;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private IServiceProvider _serviceProvider;
        private DatabaseConfiguration _dbConfig;
        private RedisCacher _cacher;

        public DonutOrionHandler(IFactory<ManagementDbContext> contextFactory,
            OrionContext orion,
            CompilerService compilerService,
            IServiceProvider serviceProvider,
            ApiService apiService,
            IntegrationService integrationService,
            RedisCacher redisCacher)
        {
            _db = contextFactory.Create();
            _orion = orion;
#pragma warning disable 4014
            _orion.FeaturesGenerated += (x)=> _orion_FeaturesGenerated(x);
#pragma warning restore 4014
            _compiler = compilerService;
            _serviceProvider = serviceProvider;
            _apiService = apiService;
            _integrationService = integrationService; 
            _cacher = redisCacher; 
            _dbConfig = DBConfig.GetGeneralDatabase();
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
            var sourceIntegration = ds.Integrations.FirstOrDefault();
            var collectioName = sourceIntegration.Collection;
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectioName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string, object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });
            source.ProgressInterval = 0.05;
            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 10);
            var entryLimit = (uint)10000;
            harvester.LimitEntries(entryLimit);

            //Create a donut and a donutRunner
            var donutMachine = DonutBuilderFactory.Create(donutType, donutContextType, sourceIntegration, _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = true;
            IDonutRunner donutRunner = DonutRunnerFactory.CreateByType(donutType, donutContextType, harvester, _dbConfig, sourceIntegration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);

            var query = OrionQuery.Factory.CreateTrainQuery(model);
            var m_id = await _orion.Query(query);
            return null;
        }
         

        public void Dispose()
        { 
        }
    }
}