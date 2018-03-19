using System.Collections.Generic;
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    public partial class OrionQuery
    {
        public OrionOp Operation { get; private set; }
        private JObject Payload { get; set; }
        public OrionQuery(OrionOp operation)
        {
            SetOperation(operation);
            Payload = new JObject();
        }

        public void SetOperation(OrionOp operation)
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