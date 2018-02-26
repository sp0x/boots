using System;
using System.Collections.Generic;
using System.IO;

namespace Netlyt.Service.Format
{
    /// <summary>
    /// 
    /// </summary>
    public interface IInputFormatter : IDisposable
    {
        string Name { get; }
        IEnumerable<dynamic> GetIterator(Stream fs, bool reset);
        IEnumerable<T> GetIterator<T>(Stream fs, bool reset) where T : class; 
        IInputFormatter Clone();
        void Reset();
        long Position();
        //double Progress { get; }
    }
}