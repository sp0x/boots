using System;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class InMemorySource : IInputSource
    {
        public int Size { get; }
        public IInputFormatter Formatter { get; }
        public string Content { get; private set; }
        public IIntegrationTypeDefinition GetTypeDefinition()
        {
            throw new NotImplementedException();
        }

        public dynamic GetNext()
        {
            throw new NotImplementedException();
        }

        public InMemorySource(string content, IInputFormatter formatter = null)
        {
            this.Content = content;
            this.Formatter = formatter;
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
    }
}