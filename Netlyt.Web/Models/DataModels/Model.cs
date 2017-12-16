using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Model
        : Entity
    {
        public long Id { get; set; }
        public User User { get; set; }
        public List<Integration> Integrations { get; set; }
        public List<Rule> Rules { get; set; }
        public string ClassifierType { get; set; }
        public int CurrentModel { get; set; }

    }
}