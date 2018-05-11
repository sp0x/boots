using System;
using System.Collections.Generic;
using System.Dynamic;
using Donut.Data.Format;
using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IInputSource
        : IDisposable, IEnumerable<object>
    {
        long Size { get; }
        System.Text.Encoding Encoding { get; set; }
        IInputFormatter Formatter { get;  }
        bool SupportsSeeking { get; }
        Dictionary<string, FieldOptionsBuilder> FieldOptions { get; set; }

        IIntegration ResolveIntegrationDefinition();
        IEnumerable<dynamic> GetIterator(Type targetType=null);
        IEnumerable<T> GetIterator<T>()
            where T : class;

        void Cleanup();

        IEnumerable<IInputSource> Shards();
        void Reset();
        void SetFormatter(IInputFormatter resolveFormatter);
    }
}