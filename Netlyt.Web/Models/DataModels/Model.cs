using System.Collections.Generic;

namespace Netlyt.Web.Models.DataModels
{
    public class Model
    {
        public long ID { get; set; }
        public User User { get; set; }
        public List<Integration> Integrations { get; set; }
        public List<Rule> Rules { get; set; }
        public string ClassifierType { get; set; }
        public int CurrentModel { get; set; }
        
    }
}