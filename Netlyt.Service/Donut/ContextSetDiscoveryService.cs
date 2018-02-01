using System.Linq;
using nvoid.db.Caching;

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

        public ContextSetDiscoveryService(DonutContext ctx)
        {
            _context = ctx;
            _setFinder = new CacheSetFinder();
            _setSource = new CacheSetSource();
        }

        public void Initialize()
        {
            foreach (var setInfo in _setFinder.FindSets(_context).Where(p => p.Setter != null))
            {
                var newSet = ((ICacheSetCollection)_context).GetOrAddSet(_setSource, setInfo.ClrType);
                newSet.Name = setInfo.Name;
                if (setInfo.Attributes.FirstOrDefault(x => x.GetType() == typeof(CacheBacking)) is CacheBacking backing)
                {
                    newSet.SetType(backing.Type);
                }
                setInfo.Setter.SetClrValue(_context, newSet);
            }
        }
    }
}