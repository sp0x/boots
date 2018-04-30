namespace Donut.Caching
{
    public class EmptyCacheMap<T> : CacheMap<T>
        where T : class
    {
        public override void Map()
        {
        }
    } 
}