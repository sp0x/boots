using System.Threading.Tasks.Dataflow;
using Donut;
using nvoid.exec.Blocks;
using Netlyt.Interfaces;
using Netlyt.Service.Models;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class DonutBlock<T> : IDonutBlock<T>
        where T : class, IIntegratedDocument
    {
        /// <summary>
        /// The flow block that's used as the root of the dataflow.
        /// </summary>
        public BaseFlowBlock<T, T> FlowBlock { get; private set; }
        /// <summary>
        /// The feature propagator block.
        /// </summary>
        public IPropagatorBlock<T, FeaturesWrapper<T>> FeaturePropagator { get; private set; }

        public DonutBlock(BaseFlowBlock<T, T> flowblock,
            IPropagatorBlock<T, FeaturesWrapper<T>> featureblock)
        {
            FlowBlock = flowblock;
            FeaturePropagator = featureblock;
        }
    }
}