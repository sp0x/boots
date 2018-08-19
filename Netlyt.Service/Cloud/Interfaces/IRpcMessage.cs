namespace Netlyt.Service.Cloud.Interfaces
{
    public interface IRpcMessage
    {
        string From { get; }
        string CorrelationId { get; }
        ulong DeliveryTag { get; set; }
    }
}