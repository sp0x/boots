using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class MongoSource : IInputSource
    {
        public IInputFormatter Formatter { get; }
        public IIntegrationTypeDefinition GetTypeDefinition()
        {
            throw new System.NotImplementedException();
        }

        public dynamic GetNext()
        {
            throw new System.NotImplementedException();
        }

        public int Size { get; }
        public Encoding Encoding { get; set; }
    }
}