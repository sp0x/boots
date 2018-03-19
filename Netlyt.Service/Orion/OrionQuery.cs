using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    public class OrionQuery
    {
        public OrionOp Operation { get; private set; }
        public OrionQuery(OrionOp operation)
        {
            SetOperation(operation);
        }

        public void SetOperation(OrionOp operation)
        {
            this.Operation = operation;
        }

        public JObject Serialize()
        {
            JObject query = new JObject();
            query.Add("op", (int)Operation);
            return query;
        }
    }
}