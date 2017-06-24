using Peeralize.Service.Integration;

namespace Peeralize.Service
{
    public interface IIntegrationDestination
    {
        void Consume();
        void Close();
        void Post(IntegratedDocument item);
        string UserId { get; }
    }
}