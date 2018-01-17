using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
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

        public override IIntegration GetTypeDefinition()
        {
            return null;
        }

        public override IEnumerable<dynamic> GetIterator()
        {
            throw new NotImplementedException();
        }

        public override void DoDispose()
        { 
            Stream?.Dispose();
        }

    }
}