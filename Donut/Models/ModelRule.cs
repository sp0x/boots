namespace Donut.Models
{
    public class ModelRule
    { 
        public long ModelId { get; set; }
        public virtual Model Model { get; set; }
        public long RuleId { get; set; }
        public virtual Rule Rule { get; set; }
    }
}