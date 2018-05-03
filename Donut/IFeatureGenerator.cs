using System.Threading.Tasks.Dataflow;
using Donut.Features;

namespace Donut
{
    public interface IFeatureGenerator<T>
    {
        IPropagatorBlock<T, FeaturesWrapper<T>> CreateFeaturesBlock();

    }
}