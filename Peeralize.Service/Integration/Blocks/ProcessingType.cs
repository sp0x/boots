using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;

namespace Peeralize.Service.Integration.Blocks
{
    public enum ProcessingType
    {
        Transform, Action
    } 
}