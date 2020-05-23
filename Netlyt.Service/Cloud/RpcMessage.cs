using Netlyt.Service.Cloud.Interfaces;

namespace Netlyt.Service.Cloud
{
    public abstract class RpcMessage : Ackable, IRpcMessage
    {
        public string From { get; protected set; }
        public string CorrelationId { get; protected set; }
        public RpcMessage(string replyTo, string correlationId, ulong deliveryTag) : base(deliveryTag)
        {
            this.From = replyTo;
            this.CorrelationId = correlationId;
        }
    }
}