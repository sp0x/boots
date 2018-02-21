using System;
using System.Linq;
using MongoDB.Driver;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.Integration;

namespace Netlyt.Service.Donut
{
    /// <summary>
    /// Service that helps find the sets that are available in a cache context.
    /// </summary>
    internal class ContextSetDiscoveryService
    {
        private DonutContext _context;
        private ICacheSetFinder _setFinder;
        private ICacheSetSource _setSource;
        private IntegrationService _integrationService;

        public ContextSetDiscoveryService(DonutContext ctx, IServiceProvider serviceProvider)
        {
            _context = ctx;
            _setFinder = new CacheSetFinder();
            _setSource = new CacheSetSource(); 
            _integrationService = serviceProvider.GetService(typeof(IntegrationService)) as IntegrationService;
        }

        public void Initialize()
        {
            var config = DBConfig.GetGeneralDatabase();
            foreach (var setInfo in _setFinder.FindSets(_context).Where(p => p.Setter != null))
            {
                var newSet = ((ISetCollection)_context).GetOrAddSet(_setSource, setInfo.ClrType);
                newSet.Name = setInfo.Name;
                if (setInfo.Attributes.FirstOrDefault(x => x.GetType() == typeof(CacheBacking)) is CacheBacking backing)
                {
                    newSet.SetType(backing.Type);
                }
                setInfo.Setter.SetClrValue(_context, newSet);
            }
            foreach(var dataSetInfo in _setFinder.FindDataSets(_context).Where(p => p.Setter != null))
            {
                var newSet = ((ISetCollection)_context).GetOrAddDataSet(_setSource, dataSetInfo.ClrType);
                //newSet.Name = dataSetInfo.Name;
                if (dataSetInfo.Attributes.FirstOrDefault(x => x.GetType() == typeof(SourceFromIntegration)) is SourceFromIntegration integrationSource)
                {
                    var integration = _integrationService.GetByName(_context.ApiAuth, integrationSource.IntegrationName);
                    if (integration == null)
                    {
                        throw new Exception(
                            $"Integration data source unavailable: {integrationSource.IntegrationName}");
                    }
                    newSet.SetSource(integration.Collection);
                    //var integration = 
                    //newSet.SetSource(integrationSource.IntegrationName);
                }
                dataSetInfo.Setter.SetClrValue(_context, newSet);
            }
        }
    }
}