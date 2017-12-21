using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Integration
        : Entity
    {
        public long Id { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }        
    }
}
