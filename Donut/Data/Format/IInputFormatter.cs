using System;
using System.Collections.Generic;
using System.IO;

namespace Donut.Data.Format
{
    public interface IInputFormatter : IDisposable
    {
        string Name { get; }
        void Reset();
        long Position(); 
        IEnumerable<dynamic> GetIterator(Stream fs, bool reset, Type targetType = null);
        IInputFormatter Clone();
        void SetFieldOptions(Dictionary<string,FieldOptionsBuilder> fieldOptions);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IInputFormatter<T> : IInputFormatter
        where T : class
    {
        //TODO: Fix this generic mess 
        IEnumerable<T> GetIterator(Stream fs, bool reset); 

        //double Progress { get; }
    }
}