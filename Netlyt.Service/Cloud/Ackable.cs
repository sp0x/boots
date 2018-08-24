using Netlyt.Service.Cloud.Interfaces;

namespace Netlyt.Service.Cloud
{
    public abstract class Ackable : IAckable
    {
        public ulong DeliveryTag { get; set; }

        public Ackable(ulong deliveryTag)
        {
            DeliveryTag = deliveryTag;
        }
    }
}