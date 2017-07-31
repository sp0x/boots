using System;
using System.IO;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class InMemorySource : IInputSource
    {
        public int Size { get; }
        public IInputFormatter Formatter { get; }
        public Stream Content { get; private set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        private object _lock = new object();
        private dynamic _cachedInstance;

        public InMemorySource(string content, IInputFormatter formatter = null)
        {
            this.Content = new MemoryStream(Encoding.GetBytes(content));
            this.Formatter = formatter;
        }

        public InMemorySource(Stream stream, IInputFormatter formatter = null)
        {
            this.Formatter = formatter;
            this.Content = stream;
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static InMemorySource Create(string payload, IInputFormatter formatter = null)
        {
            var src = new InMemorySource(payload, formatter);
            return src;
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static InMemorySource Create(Stream payload, IInputFormatter formatter = null)
        {
            var src = new InMemorySource(payload, formatter);
            return src;
        }

        public IIntegrationTypeDefinition GetTypeDefinition()
        {
            var firstInstance = _cachedInstance = Formatter.GetNext(Content, true);
            IntegrationTypeDefinition typeDef = null;
            if (firstInstance != null)
            {
                typeDef = new IntegrationTypeDefinition();
                typeDef.CodePage = Encoding.CodePage;
                typeDef.OriginType = Formatter.Name;
                typeDef.ResolveFields(firstInstance);
            }
            return typeDef;
        }

        /// <summary>
        /// Gets the next object instance
        /// </summary>
        /// <returns></returns>
        public dynamic GetNext()
        {
            lock (_lock)
            {
                dynamic lastInstance = null;
                var resetNeeded = _cachedInstance != null;
                //Probably throw?
                if (resetNeeded && Content.CanSeek)
                {
                    Content.Position = 0;
                    _cachedInstance = null;
                }
                lastInstance = Formatter.GetNext(Content, resetNeeded);
                return lastInstance;
            }
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Content?.Dispose();
                _disposed = true;
            }    
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~InMemorySource()
        {
            Dispose(false);
        }
    }
}