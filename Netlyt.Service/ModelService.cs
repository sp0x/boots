using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.Lex.Data;
using Donut.Models;
using Donut.Orion;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Models;
using Newtonsoft.Json.Linq;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service
{
    public class ModelService
    {
        private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        private IOrionContext _orion;
        private TimestampService _timestampService;
        private ManagementDbContext _dbContext;

        public ModelService(ManagementDbContext context,
            IOrionContext orionContext,
            IHttpContextAccessor ctxAccessor,
            TimestampService timestampService,
            ManagementDbContext dbContext)
        {
            _contextAccessor = ctxAccessor;
            _context = context;
            _orion = orionContext;
            _timestampService = timestampService;
            _dbContext = dbContext;
        }

        public IEnumerable<Model> GetAllForUser(User user, int page)
        {
            int pageSize = 25;
            return _context.Models
                .Where(x => x.User == user)
                .Skip(page * pageSize)
                .Take(pageSize);
        }

        public Model GetById(long id)
        {
            return _context.Models
                .Include(x=>x.DataIntegrations)
                .Include(x=>x.DonutScript)
                .Include(x=>x.Performance)
                .FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param> 
        /// <param name="integrations"></param>
        /// <param name="callbackUrl"></param>
        /// <param name="generateFeatures">If feature generation should be ran. This is only done if there are any existing integrations for this model.</param>
        /// <param name="relations"></param>
        /// <returns></returns>
        public async Task<Model> CreateModel(
            User user, 
            string name,
            IEnumerable<IIntegration> integrations, 
            string callbackUrl,
            bool generateFeatures,
            IEnumerable<FeatureGenerationRelation> relations, 
            ModelTarget target
            )
        {
            var newModel = new Model() { UseFeatures = generateFeatures, ModelName = name, Callback = callbackUrl };
            newModel.UserId = user.Id;
            newModel.Targets = new List<ModelTarget>(new ModelTarget[]{ target });

            if (integrations != null)
            {
                newModel.DataIntegrations = integrations.Select(ign => new ModelIntegration(newModel, DataIntegration.Wrap(ign))).ToList(); 
            }
            _context.Models.Add(newModel);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw new Exception("Could not create a new model!");
            }
            if (generateFeatures)
            {
                await GenerateFeatures(newModel, relations, target);
                //newModel.FeatureGenerationTasks.Add(newTask);
                //_context.FeatureGenerationTasks.Add(newTask);
                //_context.SaveChanges();
            }
            else
            {
                newModel.SetScript(GenerateDonutScriptForModel(newModel));
            }
            return newModel;
        }

        private DonutScript GenerateDonutScriptForModel(Model model)
        {
            var donutName = $"{model.ModelName}Donut";
            var rootIntegration = model.GetRootIntegration();
            var script = DonutScript.Factory.Create(donutName, model.Targets, rootIntegration);
            return script;
        }

        public async Task GenerateFeatures(Model newModel, IEnumerable<FeatureGenerationRelation> relations, ModelTarget target)
        {
            var collections = newModel.GetFeatureGenerationCollections(target);
            var query = OrionQuery.Factory.CreateFeatureDefinitionGenerationQuery(newModel, collections, relations, target);
            var result = await _orion.Query(query);
//            var newTask = new FeatureGenerationTask();
//            newTask.OrionTaskId = result["task_id"].ToString();
//            newTask.Model = newModel;
//            return newTask;
        }

        public void DeleteModel(User cruser, long id)
        {
            var targetModel = _context.Models.FirstOrDefault(x => x.User == cruser && x.Id == id);
            if (targetModel != null)
            {
                _context.Models.Remove(targetModel);
                _context.SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public FeatureGenerationTask GetFeatureGenerationTask(long id)
        {
            var model = _context.Models.Include(x => x.FeatureGenerationTasks)
                .FirstOrDefault(x=>x.Id == id);
            if (model == null) return null;
            return model.FeatureGenerationTasks.LastOrDefault();
        }

        public ModelPrepStatus GetModelPrepStatus(long id)
        {
            var model = _context.Models
                .Include(x => x.FeatureGenerationTasks)
                .FirstOrDefault(x => x.Id == id);
            if (model == null) return ModelPrepStatus.Invalid;
            if (!model.UseFeatures) return ModelPrepStatus.Done;
            else
            {
                var task = model.FeatureGenerationTasks.LastOrDefault();
                if (task == null) return ModelPrepStatus.GeneratingFeatures;
                else
                {
                    return task.Status == FeatureGenerationTaskStatus.Done
                        ? ModelPrepStatus.Done
                        : ModelPrepStatus.GeneratingFeatures;
                }
            }
        }

        public TrainingTask GetTrainingStatus(long id)
        {
            var model = _context.Models.Include(x => x.TrainingTasks)
                .FirstOrDefault(x => x.Id == id);
            if (model == null) return null;
            return model.TrainingTasks.LastOrDefault();
        }

        public void AddIntegrationsToScript(DonutScript dscript, ICollection<ModelIntegration> modelDataIntegrations)
        {
            foreach (var integration in modelDataIntegrations)
            {
                var ign = _dbContext.Integrations
                    .Include(x => x.Fields)
                    .Include(x => x.Extras)
                    .Include(x => x.APIKey)
                    .Include(x => x.AggregateKeys)
                    .Include(x => x.PublicKey)
                    .FirstOrDefault(x => x.Id == integration.IntegrationId);
                dscript.AddIntegrations(ign);
            }
        }

        public async Task<JToken> TrainModel(Model model, DataIntegration sourceIntegration)
        {
            var query = OrionQuery.Factory.CreateTrainQuery(model, sourceIntegration);
            var m_id = await _orion.Query(query);
            return m_id;
        }
    }
}