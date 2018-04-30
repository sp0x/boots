namespace Donut.Models
{
    public class ModelRule
    { 
        public long ModelId { get; set; }
        public Model Model { get; set; }
        public long RuleId { get; set; }
        public Rule Rule { get; set; }
    }
}