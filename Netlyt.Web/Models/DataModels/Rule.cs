namespace Netlyt.Web.Models.DataModels
{
    public class Rule
    {
        public long ID { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public Model Model { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}