using Netlyt.Service.Cloud.Interfaces;

namespace Netlyt.Service.Cloud
{
    public abstract class RpcMessage : IRpcMessage
    {
        public string From { get; protected set; }
        public string CorrelationId { get; protected set; }
        public ulong DeliveryTag { get; set; }

        public RpcMessage(string replyTo, string correlationId, ulong deliveryTag)
        {
            this.From = replyTo;
            this.CorrelationId = correlationId;
            this.DeliveryTag = deliveryTag;
        }
    }
}