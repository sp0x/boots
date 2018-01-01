using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Service.Ml
{
    public class Model
        : Entity
    {
        public long Id { get; set; }
        public User User { get; set; }
        public List<Integration.DataIntegration> Integrations { get; set; }
        public List<Rule> Rules { get; set; }
        public string ModelName { get; set; }
        public string ClassifierType { get; set; }
        public string CurrentModel { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }

    }
}