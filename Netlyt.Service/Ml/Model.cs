using System.Collections.Generic;
using nvoid.db.DB;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Ml
{
    public class Model
        : Entity
    {
        public long Id { get; set; }
        public User User { get; set; }
        public virtual ICollection<ModelIntegration> DataIntegrations { get; set; }
        public ICollection<ModelRule> Rules { get; set; }
        public string ModelName { get; set; }
        public string ClassifierType { get; set; }
        public string CurrentModel { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }

        public Model()
        {
            Rules = new List<ModelRule>();
            DataIntegrations = new HashSet<ModelIntegration>();
        }

    }
}