using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;

namespace Netlyt.Service.Integration.Blocks
{
    public enum ProcessingType
    {
        Transform, Action
    } 
}