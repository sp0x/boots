using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Netlyt.Interfaces;
using StackExchange.Redis;

namespace Donut.Caching
{
    public class RedisCacher  : IDisposable, IRedisCacher
    {
        // KEYS[1] = = key
        // ARGV[1] = absolute-expiration - ticks as long (-1 for none)
        // ARGV[2] = sliding-expiration - ticks as long (-1 for none)
        // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
        // ARGV[4] = data - byte[]
        // this order should not change LUA script depends on it
        private const string SetScript = (@"
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1");
        private const string AbsoluteExpirationKey = "absexp";
        private const string SlidingExpirationKey = "sldexp";
        private const string DataKey = "data";
        private const long NotPresent = -1;
        private volatile ConnectionMultiplexer _connection;
        private IDatabase _db;

        private readonly RedisCacheOptions _options;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly DistributedCacheEntryOptions _defaultOptions = new DistributedCacheEntryOptions
        {

        };

//        public static ConnectionMultiplexer GetCachingConnection()
//        {
//
//            if (_redisConnection != null) return _redisConnection;
//            var conString = $"{_cacheConfig.Host}:{_cacheConfig.Port}";
//            if (!string.IsNullOrEmpty(_cacheConfig.Arguments)) conString += "," + _cacheConfig.Arguments;
//            _redisConnection = ConnectionMultiplexer.Connect(conString);
//            return _redisConnection;
//        }

        private static Dictionary<Type, ICacheMap> _serializers = new Dictionary<Type, ICacheMap>();

        public RedisCacher(IOptions<RedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }
            _options = optionsAccessor.Value;
        }

        public byte[] Get(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return GetAndRefresh(key, getData: true);
        }

        public int GetInt(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var andRefresh = GetAndRefresh(key, getData: true);
            if (andRefresh == RedisValue.Null) return 0;
            return (int)andRefresh;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            return await GetAndRefreshAsync(key, getData: true, token: token);
        }

        public void Set(string key, int value)
        {
            Set(key, value, _defaultOptions);
        }
        public void Set(string key, int value, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            var redisKeys = new RedisKey[] { key };
            var result = _db.ScriptEvaluate(SetScript, redisKeys,
                new RedisValue[]
                {
                    absoluteExpiration?.Ticks ?? NotPresent,
                    options.SlidingExpiration?.Ticks ?? NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                    value
                });
        }

        public void SetHash(string key, HashEntry value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Connect();
            _db.HashSet(key, new HashEntry[] { value }, CommandFlags.FireAndForget);
        }
        public void SetHash<TKey, TVal>(string key, Dictionary<TKey, TVal> value)
        {
            SetHash(key, value, _defaultOptions);
        }
        public void SetHash<TKey, TVal>(string key, Dictionary<TKey, TVal> value, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            RedisKey redisKey = key;
            var fields = value.Select(
                pair => new HashEntry(pair.Key.ToString(), pair.Value.ToString()))
                .Concat(new List<HashEntry>(
                new[]{
                    new HashEntry(AbsoluteExpirationKey, absoluteExpiration?.Ticks ?? NotPresent),
                    new HashEntry(SlidingExpirationKey, options.SlidingExpiration?.Ticks ?? NotPresent)
                })).ToArray();
            //            var result = _db.ScriptEvaluate(SetScript, redisKeys,
            //                new RedisValue[]
            //                {
            //                    absoluteExpiration?.Ticks ?? NotPresent,
            //                    options.SlidingExpiration?.Ticks ?? NotPresent,
            //                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
            //                    fields
            //                }); 
            _db.HashSet(redisKey, fields);

        }

        public void SetHashes(string key, IEnumerable<HashEntry> hashElements)
        {
            SetHashes(key, hashElements, _defaultOptions);
        }
        public async Task SetHashesAsync(string key, IEnumerable<HashEntry> hashElements)
        {
            await SetHashesAsync(key, hashElements, _defaultOptions);
        }
        public void SetHashes(string key, IEnumerable<HashEntry> hashElements, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            RedisKey redisKey = key;
            _db.HashSet(redisKey, hashElements.ToArray(), CommandFlags.FireAndForget);
        }
        public async Task SetHashesAsync(string key, IEnumerable<HashEntry> hashElements, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            RedisKey redisKey = key;
            await _db.HashSetAsync(redisKey, hashElements.ToArray(), CommandFlags.FireAndForget);
        }
        public void SetAdd(string key, RedisValue value, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            RedisKey redisKey = key;
            _db.SetAdd(redisKey, value);
        }
        public void SetAdd(string key, RedisValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            SetAdd(key, value, _defaultOptions);
        }
        public void Set(string key, string value)
        {
            Set(key, value, _defaultOptions);
        }
        public void Set(string key, string value, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));
            Connect();
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            var redisKeys = new RedisKey[] { key };
            var result = _db.ScriptEvaluate(SetScript, redisKeys,
                new RedisValue[]
                {
                    absoluteExpiration?.Ticks ?? NotPresent,
                    options.SlidingExpiration?.Ticks ?? NotPresent,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                    value
                });
        }
        public void Set(string key, byte[] value, IDistributedCacheEntryOptions options)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (options == null) throw new ArgumentNullException(nameof(options));

            Connect();
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            var redisKeys = new RedisKey[] { key };
            var result = _db.ScriptEvaluate(SetScript, redisKeys,
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }

        public async Task SetAsync(string key, byte[] value, IDistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (options == null) throw new ArgumentNullException(nameof(options));

            token.ThrowIfCancellationRequested();
            await ConnectAsync(token);

            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            await _db.ScriptEvaluateAsync(SetScript, new RedisKey[] { key },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }
        /// <summary>
        /// Increments a hash member
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member">The default member for expiration managed hashes is `data`</param>
        public void Increment(string key, string member)
        {
            _db.HashIncrement(key, member);
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            GetAndRefresh(key, getData: false);
        }

        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await GetAndRefreshAsync(key, getData: false, token: token);
        }

        public IConnectionMultiplexer GetConnection()
        {
            Connect();
            return _connection;
        }
        private void Connect()
        {
            if (_connection != null)
            {
                return;
            }
            _connectionLock.Wait();
            try
            {
                if (_connection == null)
                {
                    _connection = ConnectionMultiplexer.Connect(_options.Configuration);
                    _db = _connection.GetDatabase();
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task ConnectAsync(CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();

            if (_connection != null)
            {
                return;
            }

            await _connectionLock.WaitAsync();
            try
            {
                if (_connection == null)
                {
                    _connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration);
                    _db = _connection.GetDatabase();
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private RedisValue GetAndRefresh(string key, bool getData)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Connect();
            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            RedisValue[] results;
            if (getData)
            {
                results = HashMemberGet(key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = HashMemberGet(key, AbsoluteExpirationKey, SlidingExpirationKey);
            }
            // TODO: Error handling
            if (results.Length >= 2)
            {
                MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                Refresh(key, absExpr, sldExpr);
            }
            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }
            return RedisValue.Null;
        }

        private async Task<RedisValue> GetAndRefreshAsync(string key, bool getData, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            RedisValue[] results;
            if (getData)
            {
                results = await HashMemberGetAsync(key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = await HashMemberGetAsync(key, AbsoluteExpirationKey, SlidingExpirationKey);
            }

            // TODO: Error handling
            if (results.Length >= 2)
            {
                MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                await RefreshAsync(key, absExpr, sldExpr, token);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return RedisValue.Null;
        }

        public void Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Connect();
            _db.KeyDelete(key);
        }
        /// <summary>
        /// Removes all keys matching the given key expression
        /// </summary>
        /// <param name="key"></param>
        public void RemoveAll(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Connect(); 
            var script = @"
                local keys = redis.call('keys', ARGV[1])
                if #keys > 0 then
                    return redis.call('del', unpack(keys) )
                end";
            var result = _db.ScriptEvaluate(script, values: new RedisValue[] { key }); 
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            await ConnectAsync(token);
            await _db.KeyDeleteAsync(key);
            // TODO: Error handling
        }

        private void MapMetadata(RedisValue[] results, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingExpiration)
        {
            absoluteExpiration = null;
            slidingExpiration = null;
            var absoluteExpirationTicks = (long?)results[0];
            if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NotPresent)
            {
                absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
            }
            var slidingExpirationTicks = (long?)results[1];
            if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NotPresent)
            {
                slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
            }
        }

        private void Refresh(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            TimeSpan? expr = null;
            if (sldExpr.HasValue)
            {
                if (absExpr.HasValue)
                {
                    var relExpr = absExpr.Value - DateTimeOffset.Now;
                    expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
                }
                else
                {
                    expr = sldExpr;
                }
                _db.KeyExpire(key, expr);
                // TODO: Error handling
            }
        }

        private async Task RefreshAsync(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            TimeSpan? expr = null;
            if (sldExpr.HasValue)
            {
                if (absExpr.HasValue)
                {
                    var relExpr = absExpr.Value - DateTimeOffset.Now;
                    expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
                }
                else
                {
                    expr = sldExpr;
                }
                await _db.KeyExpireAsync(key, expr);
                // TODO: Error handling
            }
        }

        private static long? GetExpirationInSeconds(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, IDistributedCacheEntryOptions options)
        {
            if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
            {
                return (long)Math.Min(
                    (absoluteExpiration.Value - creationTime).TotalSeconds,
                    options.SlidingExpiration.Value.TotalSeconds);
            }
            else if (absoluteExpiration.HasValue)
            {
                return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
            }
            else if (options.SlidingExpiration.HasValue)
            {
                return (long)options.SlidingExpiration.Value.TotalSeconds;
            }
            return null;
        }

        private static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset creationTime, IDistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                    options.AbsoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }
            var absoluteExpiration = options.AbsoluteExpiration;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = creationTime + options.AbsoluteExpirationRelativeToNow;
            }

            return absoluteExpiration;
        }

        private const string HmGetScript = (@"return redis.call('HMGET', KEYS[1], unpack(ARGV))");
        internal RedisValue[] HashMemberGet(string key, params string[] members)
        {
            var result = _db.ScriptEvaluate(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
        }
        internal async Task<RedisValue[]> HashMemberGetAsync(
            string key,
            params string[] members)
        {
            var result = await _db.ScriptEvaluateAsync(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
        }
        private static RedisValue[] GetRedisMembers(params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }

            return redisMembers;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
            }
        }

        public static void RegisterCacheMap<T, TValue>() where T : ICacheMap<TValue>, new()
            where TValue : class
        {
            var serializer = new T();
            serializer.Map();
            _serializers.Add(typeof(TValue), serializer);
        }

        public static ICacheMap<T> GetCacheMap<T>()
            where T : class
        {
            var type = typeof(T);
            if (!_serializers.ContainsKey(type))
            {
                var simpleCacheMap = new EmptyCacheMap<T>();
                _serializers.Add(type, simpleCacheMap as ICacheMap<T>);
                return simpleCacheMap;
            }
            else
            {
                var cacheSerializer = _serializers[type];
                return cacheSerializer as ICacheMap<T>;
            }
        }
        public static ICacheMap GetCacheMap(Type valueType)
        {
            if (!_serializers.ContainsKey(valueType)) return null;
            else
            {
                var cacheSerializer = _serializers[valueType];
                return cacheSerializer;
            }
        }

        public HashEntry[] GetHash(string key)
        {
            Connect();
            var entries = _db.HashGetAll(key);
            return entries;
        }

        public void SetAddAll(string fullKey, IEnumerable<RedisValue> set)
        {
            Connect();
            _db.SetAdd(fullKey, set.ToArray(), CommandFlags.FireAndForget);
        }

        public void SortedSetAddAll(string key, IEnumerable<SortedSetEntry> entries)
        {
            Connect();
            _db.SortedSetAdd(key, entries.ToArray());
        }
        /// <summary>
        /// Gets the count of elements in a set
        /// </summary>
        /// <param name="key">The name of the set</param>
        /// <returns></returns>
        public long GetSetItemCount(string key)
        {
            Connect();
            return _db.SetLength(key);
        }

        public SortedSetEntry? GetSortedSetMax(string fqkey)
        {
            Connect();
            var result = _db.ScriptEvaluate("return redis.call('ZREVRANGE', KEYS[1], 0,0, 'withscores')", new RedisKey[]
            {
                fqkey
            });
            if (!result.IsNull)
            {
                var maxValueRes = (RedisResult[])result;
                var entry = new SortedSetEntry(maxValueRes[0].ToString(), double.Parse(maxValueRes[1].ToString()));
                return entry;
            }
            return null;
        }

        public void SortedSetAdd(string kv, RedisValue value, double score)
        {
            Connect();
            _db.SortedSetAdd(kv, value, score);
        }
    }
}
