using System;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class MongoSource : IInputSource
    {
        private bool _disposed;
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Encoding = null;
                _disposed = true;
            }
        }

        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MongoSource()
        {
            Dispose(false);
        }
    }
}