using System.ComponentModel.DataAnnotations.Schema;
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
        [ForeignKey("ModelTarget")]
        public long ModelTargetId { get; set; }
        public virtual ModelTarget ModelTarget { get; set; }
    }
}