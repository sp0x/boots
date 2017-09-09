using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class StreamSource : InputSource
    {
        
        public Stream Stream { get; set; }

        public StreamSource() : base(null)
        {
            
        }

        public StreamSource(IInputFormatter formatter) : base(formatter)
        {
        }

        public override IIntegrationTypeDefinition GetTypeDefinition()
        {
            throw new System.NotImplementedException();
        }

        public override dynamic GetNext()
        {
            throw new System.NotImplementedException();
        }

          

        public override void DoDispose()
        { 
            Stream?.Dispose();
        }

    }
}