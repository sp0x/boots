using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Interfaces
{
    public interface IInputSource
        : IDisposable, IEnumerable<object>
    {
        long Size { get; }
        Encoding Encoding { get; set; }
        IInputFormatter Formatter { get;  }
        IIntegration ResolveIntegrationDefinition();
        IEnumerable<dynamic> GetIterator(Type targetType=null);
        IEnumerable<T> GetIterator<T>()
            where T : class;
    }
}