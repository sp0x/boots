using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using CachingFramework.Redis;
using MongoDB.Bson;
using nvoid.db.DB.Configuration;
using nvoid.extensions; 
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Jil;
using Jil;

namespace Peeralize.Service.Integration.Blocks
{
    public class ReduceCacheBlock
        : BaseFlowBlock<ExpandoObject, ExpandoObject>
    {
        #region "Variables" 
        private string _keyBase;
        private Func<ExpandoObject, string> _groupBySelector;
        private JilSerializer _serializer;
        private StackExchangeRedisCacheClient _cacheClient;
        private IDatabase _redis;
        /// <summary>
        /// 
        /// </summary>
        //private Func<IntegratedDocument, BsonDocument, object> _accumulator;
        /// <summary>
        /// 
        /// </summary>
        private Func<ExpandoObject, ExpandoObject> _inputProjection;
        #endregion


        public ReduceCacheBlock(
            string userId, 
            Func<ExpandoObject, string> selector,
            Func<ExpandoObject, ExpandoObject> inputProjection)
            : base(capacity: 100000, procType: ProcessingType.Transform)
        {
            _keyBase = $"reduce_cache:{userId}";
            var connection = DBConfig.GetCachingConnection();
            if (connection == null) throw new Exception("No redis connection found!"); 
            _serializer = new JilSerializer();
            _cacheClient = new StackExchangeRedisCacheClient(connection, _serializer);
            _redis = connection.GetDatabase(0);
            _groupBySelector = selector;
            _inputProjection = inputProjection;
        }

        protected override ExpandoObject OnBlockReceived(ExpandoObject intDoc)
        {
            var keyObject = _groupBySelector(intDoc);
            var key = string.Format("{0}:{1}", _keyBase, keyObject.ToString());
            var valueToCollect = _inputProjection(intDoc.Clone());  
            var hashItems = ((IDictionary<string, object>) valueToCollect)
                .Select(x => new HashEntry(x.Key, x.Value.ToString())).ToArray();
            _redis.HashSet(key, hashItems);
            //_cacheClient.Add(key, hashItems);
            return intDoc;
//            var intDocDocument = intDoc.GetDocument();
//            var isNewUser = false;
//            if (key != null)
//            {
//                if (!EntityDictionary.ContainsKey(key))
//                {
//                    var docClone = intDoc;
//                    //Ignore non valid values
//                    if (_inputProjection != null)
//                    {
//                        docClone = intDoc.Clone();
//                        _inputProjection(docClone);
//                    }
//                    EntityDictionary[key] = docClone;
//                    isNewUser = true;
//                }
//            }
//            else
//            {
//                throw new Exception("No key to group with!");
//            }//
//            RecordPageStats(intDocDocument, isNewUser);
//            var newElement = _accumulator(EntityDictionary[key], intDocDocument);
//            // return EntityDictionary[key];
//            return intDoc;
        }
    }
}