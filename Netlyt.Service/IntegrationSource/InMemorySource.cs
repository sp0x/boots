using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class InMemorySource : InputSource
    { 
        public Stream Content { get; private set; } 
        private object _lock = new object();
        private dynamic _cachedInstance;

        public InMemorySource(string content, IInputFormatter formatter = null) : base(formatter)
        {
            this.Content = new MemoryStream(Encoding.GetBytes(content)); 
        }

        public InMemorySource(Stream stream, IInputFormatter formatter = null) : base(formatter)
        { 
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

        public override IIntegrationTypeDefinition GetTypeDefinition()
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
        public override dynamic GetNext()
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

        public override void DoDispose()
        { 
            Content?.Dispose(); 
        }
 
        
    }
}