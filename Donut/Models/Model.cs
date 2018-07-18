using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Donut.Data;
using Donut.Lex.Data;
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
        public virtual ICollection<ModelTarget> Targets { get; set; }
        [ForeignKey("DonutScript")]
        public long? DonutScriptId { get; set; }
        public virtual DonutScriptInfo DonutScript {get; set;}
        public bool UseFeatures { get; set; }

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

        public Model()
        {
            Rules = new List<ModelRule>();
            DataIntegrations = new HashSet<ModelIntegration>();
            FeatureGenerationTasks = new List<FeatureGenerationTask>();
            TrainingTasks = new List<TrainingTask>();
            Targets = new List<ModelTarget>();
        }

        /// <summary>
        /// Set the model's script from a donut script and an optional assembly for the compiled script.
        /// </summary>
        /// <param name="script"></param>
        public void SetScript(DonutScript script)
        {
            DonutScript = new DonutScriptInfo(script);
            DonutScript.Model = this;
        }

        public Data.DataIntegration GetRootIntegration()
        {
            return DataIntegrations.FirstOrDefault()?.Integration;
        }

        
    }
}