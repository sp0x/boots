using System;
using System.IO;

namespace Peeralize.Service.Source
{
    public interface IInputFormatter
    {
        string Name { get; }
        dynamic GetNext(Stream fs, bool reset);
        T GetNext<T>(Stream fs, bool reset);
    }
}