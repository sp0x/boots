using System.Collections.Generic;

namespace Netlyt.Web.Models.DataModels
{
    public class Integration
    {
        public long ID { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        
    }
}