using RabbitMQ.Client;

namespace Netlyt.Service.Cloud.Interfaces
{
    public interface IRPCableExchange
    {
        IModel Channel { get; }
        string Name { get; }
    }
}