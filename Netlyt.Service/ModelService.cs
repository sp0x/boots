using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Integration;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Models;
using Donut.Orion;
using Donut.Source;
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
        private IRedisCacher _cacher;
        private IRateService _rateService;

        public ModelService(ManagementDbContext context,
            IOrionContext orionContext,
            IHttpContextAccessor ctxAccessor,
            TimestampService timestampService,
            ManagementDbContext dbContext,
            IRedisCacher cacher,
            IRateService rateService)
        {
            _contextAccessor = ctxAccessor;
            _context = context;
            _orion = orionContext;
            _timestampService = timestampService;
            _dbContext = dbContext;
            _cacher = cacher;
            _rateService = rateService;
        }

        public IEnumerable<Model> GetAllForUser(User user, int page, int pageSize = 25)
        {
            return _context.Models
                .Where(x => x.User == user)
                .Skip(page * pageSize)
                .Take(pageSize);
        }

        public Model GetById(long id, User user)
        {
            return _context.Models
                .Include(x=>x.DataIntegrations)
                .Include(x=>x.DonutScript)
                .Include(x=>x.Performance)
                .FirstOrDefault(t => t.Id == id && t.User==user);
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
        /// <param name="selectedFields"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public async Task<Model> CreateModel(
            User user, 
            string name,
            IEnumerable<IIntegration> integrations, 
            string callbackUrl,
            bool generateFeatures,
            IEnumerable<FeatureGenerationRelation> relations,
            IEnumerable<FieldDefinition> selectedFields,
            params ModelTarget[] targets
            )
        {
            var newModel = new Model() { UseFeatures = generateFeatures, ModelName = name, Callback = callbackUrl };
            newModel.UserId = user.Id;
            newModel.APIKey = user.ApiKeys.Select(x=>x.Api).FirstOrDefault();
            newModel.PublicKey = ApiAuth.Generate();
            newModel.Targets = new List<ModelTarget>(targets);
            newModel.CreatedOn = DateTime.UtcNow;

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
                await GenerateFeatures(newModel, relations, selectedFields, targets);
            }
            else
            {
                var script = GenerateDonutScriptForModel(newModel, selectedFields);
                newModel.SetScript(script);
            }

            var targetParsingQuery = OrionQuery.Factory.CreateTargetParsingQuery(newModel);
            var targetsInfo = await _orion.Query(targetParsingQuery);
            ParseTargetTasksTypes(newModel, targetsInfo);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw new Exception("Could not create a new model!");
            }
            return newModel;
        }

        private void ParseTargetTasksTypes(Model newModel, JToken targetsInfo)
        {
            var targets = newModel.Targets;
            foreach (JProperty targetNfo in targetsInfo)
            {
                var name = targetNfo.Name;
                if (name == "seq") continue;
                var type = targetNfo.Value.ToString();
                var matchingTarget = targets.FirstOrDefault(x => x.Column.Name == name);
                if (matchingTarget == null) continue;
                matchingTarget.IsRegression = type == "regression";
            }
            newModel.Targets = targets;
        }

        private DonutScript GenerateDonutScriptForModel(Model model, IEnumerable<FieldDefinition> selectedFields = null)
        {
            var donutName = $"{model.ModelName}Donut";
            var rootIntegration = model.GetRootIntegration();
            var script = DonutScript.Factory.Create(donutName, model.Targets, rootIntegration);
            if (selectedFields != null)
            {
                foreach (var fld in selectedFields)
                {
                    var isTargetFeature = model.Targets.Any(t => fld.Name == t.Column.Name);
                    //We don`t use columns that are marked as targets for features
                    if (isTargetFeature)
                    {
                        continue;
                    }
                    var feature = IdentityFeature("f_" + fld.Name, fld);
                    script.Features.Add(feature as AssignmentExpression);
                }
            }
            return script;
        }

        public IExpression IdentityFeature(string name, FieldDefinition fld)
        {
            //TODO: make this work with multiple integrations
            //var tokenizer = new FeatureToolsTokenizer(fld.Integration);
            //var parser = new DonutSyntaxReader(tokenizer.Tokenize(fld.Name));
            //IExpression expFeatureBody = parser.ReadExpression();
            var featureName = new NameExpression(name);
            var columnName = new NameExpression(fld.Name);
            var expr = new AssignmentExpression(featureName, columnName);
            return expr;
        }

        public async Task GenerateFeatures(Model newModel,
            IEnumerable<FeatureGenerationRelation> relations,
            IEnumerable<FieldDefinition> fields,
            params ModelTarget[] targets)
        {
            var integrations = newModel.DataIntegrations.Select(x => x.Integration);
            var collections = integrations.GetFeatureGenerationCollections(targets);
            var query = OrionQuery.Factory.CreateFeatureDefinitionGenerationQuery(newModel, collections, relations, fields, targets);
            var result = await _orion.Query(query);
//            var newTask = new FeatureGenerationTask();
//            newTask.OrionTaskId = result["task_id"].ToString();
//            newTask.Model = newModel;
//            return newTask;
        }

        public void DeleteModel(User cruser, long id)
        {
            var targetModel = _context.Models
                .Include(x=>x.Targets)
                .FirstOrDefault(x => x.User == cruser && x.Id == id);
            if (targetModel != null)
            {
                targetModel.Targets.Clear();
                targetModel.DonutScript = null;
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
            var model = _context.Models
                .Include(x => x.FeatureGenerationTasks)
                .FirstOrDefault(x=>x.Id == id);
            if (model == null) return null;
            return model.FeatureGenerationTasks.LastOrDefault();
        }

        public ModelPrepStatus GetModelStatus(Model model)
        {
            if (model == null) return ModelPrepStatus.Invalid;
            var buildingTasks = model.TrainingTasks.FirstOrDefault(x => x.Status == TrainingTaskStatus.InProgress 
                                                                        || x.Status == TrainingTaskStatus.Starting);
            if (buildingTasks != null)
            {
                return ModelPrepStatus.Building;
            }
            if (model.TrainingTasks == null || model.TrainingTasks.Count == 0) return ModelPrepStatus.Incomplete;
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
            var model = _context.Models
                .Include(x => x.TrainingTasks)
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

        public async Task<JToken> TrainModel(Model model, DataIntegration sourceIntegration, TrainingScript trainingScript = null)
        {
            var modelStatus = GetModelStatus(model);
            if (modelStatus == ModelPrepStatus.Building)
            {
                return new JArray(model.TrainingTasks.Where(x => x.Status == TrainingTaskStatus.InProgress ||
                                                      x.Status == TrainingTaskStatus.Starting)
                    .Select(x=>x.Id).ToArray());
            }

            var newTrainingTasks = new List<TrainingTask>();
            foreach (var target in model.Targets)
            {
                var task = CreateTrainingTask(target);
                task.Script = trainingScript;
                newTrainingTasks.Add(task);
            }
            _dbContext.SaveChanges();
            var query = OrionQuery.Factory.CreateTrainQuery(model, sourceIntegration, newTrainingTasks);
            var trainingResponse = await _orion.Query(query);
            var trainingTaskIds = trainingResponse["tids"];
            foreach (var tt in newTrainingTasks)
            {
                var tid = trainingTaskIds.Cast<JProperty>().FirstOrDefault(x => x.Name == tt.Target.Column.Name);
                if (tid != null)
                {
                    tt.TrainingTargetId = (int)tid;
                }
            }
            _dbContext.SaveChanges();
            return trainingResponse["ids"];
        }

        /// <summary>
        /// Publishes the model to our cache so that it's active for usage.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="targetPerformances"></param>
        /// <returns></returns>
        public async Task PublishModel(Model model, List<ModelTrainingPerformance> targetPerformances)
        {
            foreach (var trainingTask in model.TrainingTasks)
            {
                var target = trainingTask.Target.Column.Name;
                var performance = targetPerformances.LastOrDefault(x => x.TargetName == trainingTask.Target.Column.Name);
                var modelKey = $"builds:published:{trainingTask.Id}";
                var dict = new Dictionary<string, string>();
                dict["target"] = target;
                dict["key"] = model.APIKey.AppId;
                dict["sec"] = model.APIKey.AppSecret;
                dict["model_id"] = model.Id.ToString();
                dict["user"] = model.User.UserName;
                _cacher.SetHash(modelKey, dict);
            }
            _rateService.ApplyDefaultForUser(model.User);
            //SetUserRateCache(model.User);
        }


        private TrainingTask CreateTrainingTask(ModelTarget target)
        {
            var tt = new TrainingTask();
            tt.CreatedOn = DateTime.UtcNow;
            tt.Model = target.Model;
            tt.ModelId = target.ModelId;
            tt.Status = TrainingTaskStatus.Starting;
            tt.Target = target;
            tt.Scoring = target.Scoring;
            target.Model.TrainingTasks.Add(tt);
            return tt;
        }

        public TrainingTask GetBuildById(long buildId, User user)
        {
            var task = this._context.TrainingTasks.FirstOrDefault(x => x.Id == buildId && x.Model.User == user);
            return task;
        }
        public bool IsBuilding(Model src)
        {
            return src.TrainingTasks.Any(x => x.Status == TrainingTaskStatus.InProgress ||
                                              x.Status == TrainingTaskStatus.Starting);
        }

        public string GetTrainedEndpoint(TrainingTask srcTargetTask)
        {
            return $"http://predict.netlyt.com/";
        }
        

    }
}