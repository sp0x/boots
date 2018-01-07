using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    public class ModelRule
    {
        public long Id { get; set; }
        public long ModelId { get; set; }
        public Model Model { get; set; }
        public long RuleId { get; set; }
        public Rule Rule { get; set; }
    }
}