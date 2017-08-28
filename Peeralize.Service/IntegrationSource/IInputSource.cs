using System;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public interface IInputSource : IDisposable, IEnumerable<object>
    {
        int Size { get; }
        Encoding Encoding { get; set; }
        IInputFormatter Formatter { get;  } 
        IIntegrationTypeDefinition GetTypeDefinition();
        dynamic GetNext();
    }
}