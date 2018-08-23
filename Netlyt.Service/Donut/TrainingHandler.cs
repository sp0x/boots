using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Donut;
using Donut.Models;
using Donut.Orion;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service.Cloud;
using Netlyt.Service.Data;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Donut
{
    public class TrainingHandler
    {
        private IOrionContext _orion;
        private ManagementDbContext _db;
        private IEmailSender _emailService;
        private ModelService _modelService;
        private INotificationService _notifications;

        #region Events
        
        #endregion

        public TrainingHandler(IFactory<ManagementDbContext> contextFactory,
            IOrionContext orion,
            IEmailSender emailSender,
            ModelService modelService,
            INotificationService notifications)
        {
            _db = contextFactory.Create();
            _notifications = notifications;
            _orion = orion;
#pragma warning disable 4014
#pragma warning restore 4014
            _emailService = emailSender;
            _modelService = modelService;
        }

        public async Task HandleComplete(JObject trainingCompleteNotification)
        {
            try
            {
                var trainingResult = trainingCompleteNotification["result"];
                var trainingParams = trainingCompleteNotification["params"];
                if (trainingParams == null) return;
                var modelId = long.Parse(trainingParams["model_id"].ToString());
                var taskIds = trainingParams["tasks"].Select(x => long.Parse(x.ToString()));
                Model model = _db.Models
                    .Include(x => x.DataIntegrations)
                    .Include(x => x.TrainingTasks)
                    .Include(x => x.User)
                    .FirstOrDefault(x => x.Id == modelId);

                if (model.User == null)
                {
                    model.User = _db.Users.FirstOrDefault(x => x.Id == model.UserId);
                }
                if (model.User == null) return;
                var completedTasks = model.TrainingTasks.Where(x => taskIds.Any(y => y == x.Id));
                foreach (var task in completedTasks)
                {
                    task.Status = TrainingTaskStatus.Done;
                    task.UpdatedOn = DateTime.UtcNow;
                }

                var targetPerformances = ParseTrainingResult(trainingResult, model).ToList();
                _db.SaveChanges();
                await _modelService.PublishModel(model, targetPerformances);
                //Notify user that training is complete
                var endpoint = "http://dev.netlyt.com/oneclick/" + model.Id;
                var mailMessage = $"Model training for {model.ModelName} is now complete." +
                                  $"Get your results here: {endpoint}";
                await _emailService.SendEmailAsync(model.User.Email, "Training complete.", mailMessage);
                _notifications.SendModelTrained(model, targetPerformances);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private IEnumerable<ModelTrainingPerformance> ParseTrainingResult(JToken results, Model model)
        {
            //We go over the target performances  and set the task results.
            foreach (JProperty resultProp in results)
            {
                var targetName = resultProp.Name;
                var result = resultProp.Value[0];
                string modelTypeInfo = result["model"].ToString();
                string scoring = result["scoring"]?.ToString();
                var perf = result["performance"];
                var perfObj = new ModelTrainingPerformance();
                perfObj.ModelId = (long)perf["ModelId"];
                perfObj.Accuracy = (float)perf["Accuracy"];
                perfObj.ReportUrl = perf["ReportUrl"].ToString();
                perfObj.TaskType = perf["TaskType"].ToString();
                perfObj.Scoring = perf["Scoring"].ToString();
                perfObj.TestResultsUrl = perf["TestResultsUrl"].ToString();
                perfObj.AdvancedReport = perf["AdvancedReport"].ToString();
                perfObj.FeatureImportance = perf["FeatureImportance"].ToString();
                var targetTask = model.TrainingTasks.FirstOrDefault(x => x.Target.Column.Name == targetName);
                if (targetTask != null)
                {
                    perfObj.TargetName = targetTask.Target.Column.Name;
                    targetTask.TypeInfo = modelTypeInfo;
                    targetTask.Performance = perfObj;
                    targetTask.Scoring = scoring;
                }
                yield return perfObj;
            }
        }
    }
}
