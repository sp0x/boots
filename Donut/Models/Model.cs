using System;
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
        [ForeignKey("APIKey")]
        public long APIKeyId { get; set; }
        public virtual ApiAuth APIKey { get; set; }
        public long? PublicKeyId { get; set; }
        public virtual ApiAuth PublicKey { get; set; }

        public virtual ModelTrainingPerformance Performance { get; set; }
        public virtual ICollection<ModelIntegration> DataIntegrations { get; set; }
        public virtual ICollection<ModelRule> Rules { get; set; }
        public virtual ICollection<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public virtual ICollection<TrainingTask> TrainingTasks { get; set; }
        
        public string Grouping { get; set; }
        public DateTime CreatedOn { get; set; }
        public virtual ICollection<ModelTarget> Targets { get; set; }
        [ForeignKey("DonutScript")]
        public long? DonutScriptId { get; set; }
        public virtual DonutScriptInfo DonutScript {get; set;}
        public bool UseFeatures { get; set; }
        //public string Scoring { get; set; } = "r2";

        public string ModelName { get; set; }
        /// <summary>
        /// A pickled scikit model's name
        /// </summary>
        public string CurrentModel { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }
        public virtual ICollection<Permission> Permissions{ get; set; }
        public bool IsRemote { get; set; }
        public long? RemoteId { get; set; }


        public Model()
        {
            Rules = new List<ModelRule>();
            DataIntegrations = new HashSet<ModelIntegration>();
            FeatureGenerationTasks = new List<FeatureGenerationTask>();
            TrainingTasks = new List<TrainingTask>();
            Targets = new List<ModelTarget>();
            Permissions = new HashSet<Permission>();

        }

        /// <summary>
        /// Set the model's script from a donut script and an optional assembly for the compiled script.
        /// </summary>
        /// <param name="script"></param>
        public DonutScriptInfo SetScript(IDonutScript script)
        {
            DonutScript = new DonutScriptInfo(script);
            DonutScript.Model = this;
            return DonutScript;
        }

        public Data.DataIntegration GetRootIntegration()
        {
            return DataIntegrations.FirstOrDefault()?.Integration;
        }

        public string GetFeaturesCollection()
        {
            var integration = GetRootIntegration();
            var rootCollection = integration.GetModelSourceCollection(this);
            return rootCollection;
        }
    }
}