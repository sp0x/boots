﻿using System;
using System.IO;

namespace Peeralize.Service.Source
{
    /// <summary>
    /// 
    /// </summary>
    public interface IInputFormatter : IDisposable
    {
        string Name { get; }
        dynamic GetNext(Stream fs, bool reset);
        T GetNext<T>(Stream fs, bool reset) where T : class;
        IInputFormatter Clone();
        double Progress { get; }
    }
}