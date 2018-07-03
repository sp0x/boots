using MongoDB.Bson;

namespace Donut.Data
{
    public class TargetConstraint
    {
        public TargetConstraintType Type { get; set; }
        public string Key { get; set; }
        public BsonDocument After { get; set; }
        public BsonDocument Before { get; set; }
    }
}