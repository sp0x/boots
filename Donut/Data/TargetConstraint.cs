using MongoDB.Bson;

namespace Donut.Data
{
    public class TargetConstraint
    {
        public long Id { get; set; }
        public TargetConstraintType Type { get; set; }
        public string Key { get; set; }
        public virtual TimeConstraint After { get; set; }
        public virtual TimeConstraint Before { get; set; }
    }
}