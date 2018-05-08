using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Netlyt.Interfaces
{
    public interface IRedisCacher
    {
        void Dispose();
        byte[] Get(string key);
        Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));
        HashEntry[] GetHash(string key);
        int GetInt(string key);
        SortedSetEntry? GetSortedSetMax(string fqkey);
        void Increment(string key, string member);
        void Refresh(string key);
        Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));
        void Remove(string key);
        void RemoveAll(string key);
        Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));
        void Set(string key, byte[] value, IDistributedCacheEntryOptions options);
        void Set(string key, int value);
        void Set(string key, int value, IDistributedCacheEntryOptions options);
        void Set(string key, string value);
        void Set(string key, string value, IDistributedCacheEntryOptions options);
        void SetAdd(string key, RedisValue value);
        void SetAdd(string key, RedisValue value, IDistributedCacheEntryOptions options);
        void SetAddAll(string fullKey, IEnumerable<RedisValue> set);
        Task SetAsync(string key, byte[] value, IDistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));
        void SetHash(string key, HashEntry value);
        void SetHash<TKey, TVal>(string key, Dictionary<TKey, TVal> value);
        void SetHash<TKey, TVal>(string key, Dictionary<TKey, TVal> value, IDistributedCacheEntryOptions options);
        void SetHashes(string key, IEnumerable<HashEntry> hashElements);
        void SetHashes(string key, IEnumerable<HashEntry> hashElements, IDistributedCacheEntryOptions options);
        Task SetHashesAsync(string key, IEnumerable<HashEntry> hashElements);
        Task SetHashesAsync(string key, IEnumerable<HashEntry> hashElements, IDistributedCacheEntryOptions options);
        long GetSetItemCount(string key);
        void SortedSetAdd(string kv, RedisValue value, double score);
        void SortedSetAddAll(string key, IEnumerable<SortedSetEntry> entries);
    }
}