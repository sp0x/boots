using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class StreamSource : InputSource
    {
        
        public Stream Stream { get; set; }

        public StreamSource() : base()
        {
            
        }

        public override IIntegration ResolveIntegrationDefinition()
        {
            return null;
        }
        public override IEnumerable<T> GetIterator<T>()
        {
            return GetIterator(typeof(T)).Cast<T>();
        }

        public override IEnumerable<dynamic> GetIterator(Type targetType = null)
        {
            throw new NotImplementedException();
        }

        public override void DoDispose()
        { 
            Stream?.Dispose();
        }

    }
}