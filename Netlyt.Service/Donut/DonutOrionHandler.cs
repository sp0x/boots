using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Lex.Data;
using Donut.Orion;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service.Data;
using Newtonsoft.Json.Linq;
using Model = Donut.Models.Model;

namespace Netlyt.Service.Donut
{
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
        private ModelService _modelService;
        private IDonutService _donutService;

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
            _modelService = serviceProvider.GetService(typeof(ModelService)) as ModelService;
            _donutService = serviceProvider.GetService(typeof(IDonutService)) as IDonutService;
            _integrationService = integrationService;
            _cacher = redisCacher;
            _dbConfig = dbc;
            Console.WriteLine("Initialized orion handler..");
        }

        private async Task _orion_TrainingComplete(JObject trainingCompleteNotification)
        {
            var handler = _serviceProvider.GetService(typeof(TrainingHandler)) as TrainingHandler;
            await handler.HandleComplete(trainingCompleteNotification);
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
                .Include(x=>x.AggregateKeys)
                .FirstOrDefault(x => x.Id == modelIntegration.IntegrationId);
            if (sourceIntegration == null)
            {
                throw new InvalidOperationException("Model has no integrations!");
            }
            try
            {
                var featureBodies = features.Select(x => x.ToString()).ToArray();
                string donutName = $"{model.ModelName}Donut";
                DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, model.Targets, sourceIntegration, featureBodies);
                _modelService.AddIntegrationsToScript(dscript, model.DataIntegrations);
                
                Type donutType, donutContextType, donutFEmitterType;
                _compiler.SetModel(model);
                var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType,
                    out donutFEmitterType);
                model.SetScript(dscript);
                _db.SaveChanges();
                ModelFeaturesGenerated?.Invoke(this, model);
                //We got the model, start training
                var trainingResult = await ExtractAndTrainFeatures(model, dscript);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Could not compile donut or error during training." + ex.Message);
                Console.WriteLine("Could not compile donut or error during training." + ex.Message);
            }
        }

        private async Task<JToken> ExtractAndTrainFeatures(
            Model model,
            DonutScript script)
        {
            var sourceIntegration = script.Integrations.FirstOrDefault();
            sourceIntegration = _db.Integrations
                .Include(x => x.Fields)
                .Include(x => x.Extras)
                .Include(x => x.Models)
                .Include(x => x.APIKey)
                .Include(x=>x.AggregateKeys)
                .FirstOrDefault(x => x.Id == sourceIntegration.Id);
            //Run the donut to extract features
            var result = await _donutService.RunExtraction(script, sourceIntegration, _serviceProvider);
            var t_id = await _modelService.TrainModel(model, model.User, sourceIntegration);
            return t_id;
        }


        public void Dispose()
        {
        }
    }
}