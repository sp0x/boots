using System.Threading.Tasks.Dataflow;

namespace Donut
{
    public interface IFeatureGenerator<T>
    {
        IPropagatorBlock<T, FeaturesWrapper<T>> CreateFeaturesBlock();

    }
}