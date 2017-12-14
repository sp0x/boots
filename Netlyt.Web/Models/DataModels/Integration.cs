using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Integration: Entity
    {
        public long ID { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        
    }
}