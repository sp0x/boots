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
        IAsyncCursorSource<BsonDocument> _finder = null;
        IAsyncCursor<BsonDocument> _cursor;
        private BsonDocument[] _elementCache;
        private int _position = -1;
        
        
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
                _elementCache = null;
                _cursor.Dispose();
            }
        }

        
        public string Name { get; } = "BsonFormatter";
        public dynamic GetNext(Stream fs, bool reset)
        {
            throw new NotImplementedException();
        }
        public long Position()
        {
            return _passedElements + _position;
        }

        public dynamic GetNext(IAsyncCursorSource<BsonDocument> cursorSource, bool reset)
        {
            _finder = cursorSource;
//            if (_size == 0 || reset)
//            {
//                _size = _finder.Count();
//            } 
            lock (_lock)
            {

                if (_cursor == null || reset)
                {
                    _cursor = cursorSource.ToCursor();
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToArray() : new BsonDocument[] { };
                }
                if (_elementCache != null && _position >= _elementCache.Count())
                {
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToArray() : new BsonDocument[] { };
                    _passedElements += _position;
                    _position = 0;

                }
            }
            if (_position == -1) _position = 0;
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
    }
}