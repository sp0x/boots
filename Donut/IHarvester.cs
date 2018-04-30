using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut.Integration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Blocks;

namespace Donut
{
    public interface IHarvester<TDocument> where TDocument : class
    {
        IFlowBlock<TDocument> Destination { get; }
        HashSet<IIntegrationSet> IntegrationSets { get; }
        uint ThreadCount { get; }

        IIntegration AddIntegrationSource(IInputSource inputSource, ApiAuth appAuth, string name, string outputCollection = null);
        void AddIntegrationSource(IInputSource inputSource, IIntegration integration);
        IHarvester<TDocument> AddType(IIntegration input, IInputSource source);
        TimeSpan ElapsedTime();
        void LimitEntries(uint max);
        void LimitShards(uint max);
        Task<HarvesterResult> ReadAll(ITargetBlock<ExpandoObject> target, CancellationToken? cancellationToken = null);
        void Reset();
        Task<HarvesterResult> Run(CancellationToken? cancellationToken = null);
        IHarvester<TDocument> SetDestination(IFlowBlock<TDocument> dest);
    }
}