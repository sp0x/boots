using MongoDB.Bson;
using nvoid.db.DB;

namespace Peeralize.Service.Integration
{
    public class IntegratedDocument : Entity<int>
    {
        public BsonDocument Document { get; set; }
        public BsonDocument Reserved { get; set; }
        public string UserId { get; set; }
        public string TypeId { get; set; }

        public IntegratedDocument()
        {
            Reserved = new BsonDocument();
        }

        public void SetDocument(dynamic doc)
        {
            Document = ((object)doc).ToBsonDocument(); 
        }
    }
     
}