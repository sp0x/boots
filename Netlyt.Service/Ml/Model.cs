using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using nvoid.db.DB;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Models;
using Netlyt.Service.Orion;

namespace Netlyt.Service.Ml
{
    public class Model
        : Entity
    {
        public long Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }
        public ModelTrainingPerformance Performance { get; set; }
        public virtual ICollection<ModelIntegration> DataIntegrations { get; set; }
        public virtual ICollection<ModelRule> Rules { get; set; }
        public virtual ICollection<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public virtual ICollection<TrainingTask> TrainingTasks { get; set; }
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
        public string TargetAttribute { get; set; }

        public Model()
        {
            Rules = new List<ModelRule>();
            DataIntegrations = new HashSet<ModelIntegration>();
            FeatureGenerationTasks = new List<FeatureGenerationTask>();
            TrainingTasks = new List<TrainingTask>();
        }

        public DataIntegration GetRootIntegration()
        {
            return DataIntegrations.FirstOrDefault()?.Integration;
        }

        public IEnumerable<FeatureGenerationCollectionOptions> GetFeatureGenerationCollections(string targetAttribute)
        {
            var timestampservice = new TimestampService(null);
            foreach (var integration in DataIntegrations)
            {
                var ign = integration.Integration;
                var ignTimestampColumn = !string.IsNullOrEmpty(ign.DataTimestampColumn) ? ign.DataTimestampColumn : timestampservice.Discover(ign);
                var fields = ign.Fields;
                InternalEntity intEntity = null;
                if (fields.Any(x => x.Name == targetAttribute))
                {
                    intEntity = new InternalEntity()
                    {
                        Name = targetAttribute
                    };
                }
                var colOptions = new FeatureGenerationCollectionOptions()
                {
                    Collection = ign.Collection,
                    Name = ign.Name,
                    TimestampField = ignTimestampColumn,
                    InternalEntity = intEntity,
                    Integration = ign
                    //Other parameters are ignored for now
                };
                yield return colOptions;
            }
        }
    }
}