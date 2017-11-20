using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public interface IInputSource : IDisposable, IEnumerable<object>
    {
        long Size { get; }
        Encoding Encoding { get; set; }
        IInputFormatter Formatter { get;  } 
        IIntegrationTypeDefinition GetTypeDefinition();
        dynamic GetNext();
    }
}