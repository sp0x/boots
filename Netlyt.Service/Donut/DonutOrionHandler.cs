using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Donut;
using Donut.Data.Format;
using Donut.FeatureGeneration;
using Donut.IntegrationSource;
using Donut.Lex.Data;
using Donut.Orion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service.Data;
using Newtonsoft.Json.Linq;
using Model = Donut.Models.Model;

namespace Netlyt.Service.Donut
{
    public static class Extensions
    {
        public static DonutOrionHandler GetOrionHandler(this IServiceProvider services)
        {
            var svc = services.GetService<DonutOrionHandler>();
            if (svc == null) throw new Exception("Orion handler service not registered!");
            return svc;
        }
    }
    public class DonutOrionHandler : IDisposable
    {
        private IOrionContext _orion;
        private ManagementDbContext _db;
        private CompilerService _compiler;
        private ApiService _apiService;
        private IIntegrationService _integrationService;
        private IServiceProvider _serviceProvider;
        private IDatabaseConfiguration _dbConfig;
        private IRedisCacher _cacher;
        private IEmailSender _emailService;

        #region Events

        public event EventHandler<Model> ModelFeaturesGenerated;
        #endregion

        public DonutOrionHandler(IFactory<ManagementDbContext> contextFactory,
            IOrionContext orion,
            CompilerService compilerService,
            IServiceProvider serviceProvider,
            ApiService apiService,
            IIntegrationService integrationService,
            IDatabaseConfiguration dbc,
            IRedisCacher redisCacher,
            IEmailSender emailSender)
        {
            _db = contextFactory.Create();
            _orion = orion;
#pragma warning disable 4014
            _orion.FeaturesGenerated += (x) => _orion_FeaturesGenerated(x);
            _orion.TrainingComplete += (x) => _orion_TrainingComplete(x);
#pragma warning restore 4014
            _compiler = compilerService;
            _serviceProvider = serviceProvider;
            _apiService = apiService;
            _integrationService = integrationService;
            _cacher = redisCacher;
            _dbConfig = dbc;
            _emailService = emailSender;
            Console.WriteLine("Initialized orion handler..");
        }

        private async void _orion_TrainingComplete(JObject trainingCompleteNotification)
        {
            try
            {
                var trResult = trainingCompleteNotification["params"];
                if (trResult == null) return;
                var taskId = trResult["task_id"].ToString();
                var modelId = long.Parse(trResult["model_id"].ToString());
                Model model = _db.Models
                    .Include(x => x.DataIntegrations)
                    .Include(x => x.User)
                    .FirstOrDefault(x => x.Id == modelId);
                if (model.User == null)
                {
                    model.User = _db.Users.FirstOrDefault(x => x.Id == model.UserId);
                }
                if (model.User == null) return;
                //Notify user that training is complete
                var endpoint = "http://dev.netlyt.com/oneclick/" + model.Id;
                var mailMessage = $"Model training for {model.ModelName} is now complete." +
                    $"Get your results here: {endpoint}";
                await _emailService.SendEmailAsync(model.User.Email, "Training complete.", mailMessage);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
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
            Model model = _db.Models
                .Include(x => x.DataIntegrations)
                .Include(x => x.User)
                .FirstOrDefault(x => x.Id == modelId);
            if (model.User == null)
            {
                model.User = _db.Users.FirstOrDefault(x => x.Id == model.UserId);
            }

            if (model == null) return;
            var modelIntegration = model.DataIntegrations.FirstOrDefault();
            var sourceIntegration = _db.Integrations
                .Include(x => x.APIKey)
                .Include(x => x.Fields)
                .Include(x => x.Models)
                .Include(x => x.Owner)
                .Include(x => x.PublicKey)
                .FirstOrDefault(x => x.Id == modelIntegration.IntegrationId);
            if (sourceIntegration == null)
            {
                throw new InvalidOperationException("Model has no integrations!");
            }
            try
            {
                var featureBodies = features.Select(x => x.ToString()).ToArray();
                string donutName = $"{model.ModelName}Donut";
                DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, model.TargetAttribute, sourceIntegration, featureBodies);
                dscript.TargetAttribute = model.TargetAttribute;
                foreach (var integration in model.DataIntegrations)
                {
                    var ign = _db.Integrations
                        .Include(x => x.Fields)
                        .Include(x => x.Extras)
                        .Include(x => x.APIKey)
                        .Include(x => x.PublicKey)
                        .FirstOrDefault(x => x.Id == integration.IntegrationId);
                    dscript.AddIntegrations(ign);
                }
                Type donutType, donutContextType, donutFEmitterType;
                _compiler.SetModel(model);
                var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType,
                    out donutFEmitterType);
                model.DonutScript = new DonutScriptInfo(dscript);
                model.DonutScript.AssemblyPath = assembly.Location;
                model.DonutScript.Model = model;
                _db.SaveChanges();
                ModelFeaturesGenerated?.Invoke(this, model);
                //We got the model, start training
                var trainingResult = await TrainGeneratedFeatures(model, dscript, assembly, donutType, donutContextType,
                    donutFEmitterType);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Could not compile donut or error during training." + ex.Message);
                Console.WriteLine("Could not compile donut or error during training." + ex.Message);
            }
        }

        private async Task<JToken> TrainGeneratedFeatures(Model model,
            DonutScript ds,
            Assembly assembly,
            Type donutType,
            Type donutContextType,
            Type donutFEmitterType)
        {
            var sourceIntegration = ds.Integrations.FirstOrDefault();
            sourceIntegration = _db.Integrations
                .Include(x => x.Fields)
                .Include(x => x.Extras)
                .Include(x => x.Models)
                .Include(x => x.APIKey)
                .FirstOrDefault(x => x.Id == sourceIntegration.Id);
            var appAuth = sourceIntegration.APIKey;
            if (appAuth == null) appAuth = sourceIntegration.PublicKey;

            var collectioName = sourceIntegration.Collection;
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectioName, new BsonFormatter<ExpandoObject>());
            //source.ProgressInterval = 0.05;
            var harvester = new Harvester<IntegratedDocument>(10);
            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(donutType, donutContextType, sourceIntegration, _cacher, _serviceProvider);
            harvester.AddIntegrationSource(source, sourceIntegration);//source, appAuth, "SomeIntegrationName3");
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = true;
            donut.SkipFeatureExtraction = true;
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType<IntegratedDocument, IntegratedDocument>(donutType, donutContextType,
                harvester, _dbConfig, sourceIntegration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);

            var query = OrionQuery.Factory.CreateTrainQuery(model, sourceIntegration);
            var m_id = await _orion.Query(query);
            return m_id;
        }


        public void Dispose()
        {
        }
    }
}