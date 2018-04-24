using System.Threading.Tasks.Dataflow;
using Donut;
using Netlyt.Interfaces;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    public interface IDonutBlock<T>
        where T : IIntegratedDocument
    {
        IPropagatorBlock<T, FeaturesWrapper<T>> FeaturePropagator { get; }
        BaseFlowBlock<T, T> FlowBlock { get; }
    }
}