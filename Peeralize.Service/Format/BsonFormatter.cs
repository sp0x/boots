using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Peeralize.Service.Source;

namespace Peeralize.Service.Format
{
    public class BsonFormatter : IInputFormatter
    {
        IFindFluent<BsonDocument, BsonDocument> _finder = null;
        IAsyncCursor<BsonDocument> _cursor;
        private BsonDocument[] _elementCache;
        private int _position = 0;
        
        private long _size = 0;
        private object _lock;
        private long _passedElements = 0;

        public BsonFormatter()
        {
            _lock = new object();
        }

        public void Dispose()
        {
            if (_cursor != null)
            {
                _cursor.Dispose();
            }
        }

        
        public string Name { get; } = "BsonFormatter";
        public dynamic GetNext(Stream fs, bool reset)
        {
            throw new NotImplementedException();
        }
        private long GetTotalPosition()
        {
            return _passedElements + _position;
        }

        public dynamic GetNext(IFindFluent<BsonDocument, BsonDocument> finder, bool reset)
        {
            _finder = finder;
            if (_size == 0 || reset)
            {
                _size = _finder.Count();
            } 
            lock (_lock)
            {

                if (_cursor == null || reset)
                {
                    _cursor = finder.ToCursor();
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToArray() : new BsonDocument[] { };
                }
                if (_elementCache != null && _position >= _elementCache.Count())
                {
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToArray() : new BsonDocument[] { };
                    _passedElements += _position;
                    _position = 0;
#if DEBUG
                    Debug.WriteLine($"Bson progress: %{Progress:0.0000} of {_size}");
#endif
                }
            } 
            //if (reset) cursor.Dispose();
            if (_elementCache!=null && _elementCache.Count() > _position)
            {
                var element = _elementCache[_position];
                Interlocked.Increment(ref _position);
                //var output = BsonSerializer.Deserialize<ExpandoObject>(element);
                return element;
            }
            else
            {
                return null;
            }
        }
        public T GetNext<T>(Stream fs, bool reset) where T : class
        {
            throw new NotImplementedException();
        }

        public IInputFormatter Clone()
        {
            var formatter = new BsonFormatter();
            formatter._finder = _finder;
            formatter._cursor = null;
            return formatter;
        }

        public double Progress
        {
            get
            {
                return 100 * ((double)GetTotalPosition() / Math.Max(1, _size));
            }
        } 
    }
}