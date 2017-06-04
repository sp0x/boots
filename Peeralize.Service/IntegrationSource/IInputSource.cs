using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public interface IInputSource
    {
        int Size { get; }
        IInputFormatter Formatter { get;  }
        IIntegrationTypeDefinition GetTypeDefinition();
        dynamic GetNext();
    }
}