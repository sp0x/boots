using System.Collections.Generic;
using nvoid.db.DB;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Models;

namespace Netlyt.Service.Ml
{
    
    public class Model
        : Entity
    {
        public long Id { get; set; }
        public User User { get; set; }
        public virtual ICollection<ModelIntegration> DataIntegrations { get; set; }
        public virtual ICollection<ModelRule> Rules { get; set; }
        public virtual ICollection<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public DonutScriptInfo DonutScript {get; set;}
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
        }

    }
}