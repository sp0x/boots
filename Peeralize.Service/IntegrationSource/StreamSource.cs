using System;
using System.IO;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class StreamSource : IInputSource
    {
        private bool _disposed;

        public IInputFormatter Formatter { get; private set; }
        public Stream Stream { get; set; }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Stream?.Dispose(); 
                _disposed = true;
            }
        }

        ~StreamSource()
        {
            Dispose(false);
        }
    }
}