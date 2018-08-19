﻿using System.Threading.Tasks;
using Netlyt.Interfaces.Models;

namespace Netlyt.Client.Slave
{
    public interface ISlaveConnector
    {
        NetlytNode Node { get; }
        ApiRateLimit Quota { get; }
        bool Running { get; set; }

        Task Run();
        void Send(string message);
    }
}