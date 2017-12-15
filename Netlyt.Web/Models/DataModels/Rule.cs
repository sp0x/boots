using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Rule
        : Entity
    {
        public long Id { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public Model Model { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}