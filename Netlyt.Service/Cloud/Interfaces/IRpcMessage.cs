namespace Netlyt.Service.Cloud.Interfaces
{
    public interface IRpcMessage : IAckable
    {
        string From { get; }
        string CorrelationId { get; }
    }

    public interface IAckable
    {
        ulong DeliveryTag { get; set; }
    }
}