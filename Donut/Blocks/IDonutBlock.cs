using System.Threading.Tasks.Dataflow;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Blocks;

namespace Donut.Blocks
{
    public interface IDonutBlock<T>
        where T : IIntegratedDocument
    {
        IPropagatorBlock<T, FeaturesWrapper<T>> FeaturePropagator { get; }
        BaseFlowBlock<T, T> FlowBlock { get; }
    }
}