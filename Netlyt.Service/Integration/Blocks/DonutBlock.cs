using System.Threading.Tasks.Dataflow;
using nvoid.exec.Blocks;
using Netlyt.Service.Models;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class DonutBlock
    {
        /// <summary>
        /// The flow block that's used as the root of the dataflow.
        /// </summary>
        public BaseFlowBlock<IntegratedDocument, IntegratedDocument> FlowBlock { get; private set; }
        /// <summary>
        /// The feature propagator block.
        /// </summary>
        public IPropagatorBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>> FeaturePropagator { get; private set; }

        public DonutBlock(BaseFlowBlock<IntegratedDocument, IntegratedDocument> flowblock,
            IPropagatorBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>> featureblock)
        {
            FlowBlock = flowblock;
            FeaturePropagator = featureblock;
        }
    }
}