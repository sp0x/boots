using System.Threading.Tasks;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutRunner<TData>
        where TData : class, IIntegratedDocument
    {
        Task<IHarvesterResult> Run(IDonutfile donut, IFeatureGenerator<TData> featureGenerator);
    }
    public interface IDonutRunner<TDonut, TData> : IDonutRunner<TData>
        where TData: class, IIntegratedDocument
    {
        Task<IHarvesterResult> Run(TDonut donut, IFeatureGenerator<TData> getFeatureGenerator);
    }
}