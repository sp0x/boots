using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq; 
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut.Models
{
    public class Model
    {
        public long Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public virtual ModelTrainingPerformance Performance { get; set; }
        public virtual ICollection<ModelIntegration> DataIntegrations { get; set; }
        public virtual ICollection<ModelRule> Rules { get; set; }
        public virtual ICollection<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public virtual ICollection<TrainingTask> TrainingTasks { get; set; }
        [ForeignKey("DonutScript")]
        public long? DonutScriptId { get; set; }
        public virtual DonutScriptInfo DonutScript {get; set;}

        public string ModelName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ClassifierType { get; set; }
        /// <summary>
        /// A pickled scikit model's name
        /// </summary>
        public string CurrentModel { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }
        public string TargetAttribute { get; set; }

        public Model()
        {
            Rules = new List<ModelRule>();
            DataIntegrations = new HashSet<ModelIntegration>();
            FeatureGenerationTasks = new List<FeatureGenerationTask>();
            TrainingTasks = new List<TrainingTask>();
        }

        public Data.DataIntegration GetRootIntegration()
        {
            return DataIntegrations.FirstOrDefault()?.Integration;
        }

        
    }
}