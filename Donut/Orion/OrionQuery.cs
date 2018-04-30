using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    public partial class OrionQuery
    {
        public global::Donut.Orion.OrionOp Operation { get; private set; }
        private JObject Payload { get; set; }
        public OrionQuery(global::Donut.Orion.OrionOp operation)
        {
            SetOperation(operation);
            Payload = new JObject();
        }

        public void SetOperation(global::Donut.Orion.OrionOp operation)
        {
            this.Operation = operation;
        }

        public JToken this[string key]
        {
            get
            {
                return Payload[key];
            }
            set { Payload[key] = value; }
        }

        public JObject Serialize()
        {
            JObject query = new JObject();
            query.Add("op", (int)Operation);
            if (Payload.Count > 0)
            {
                foreach (var item in Payload)
                {
                    query[item.Key] = item.Value;
                }
            }
            return query;
        }

        
    }
}