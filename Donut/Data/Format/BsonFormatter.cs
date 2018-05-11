using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Netlyt.Interfaces;

namespace Donut.Data.Format
{
    public class BsonFormatter<T> : InputFormatter<T>
        where T : class
    {
        IAsyncCursorSource<BsonDocument> _finder = null;
        IAsyncCursor<BsonDocument> _cursor;
        private List<BsonDocument> _elementCache;
        private int _position = -1;

        private object _lock;
        private long _passedElements = 0;

        public BsonFormatter()
        {
            _lock = new object();
            Name = "BsonFormatter";
        }

        public override void Dispose()
        {
            if (_cursor != null)
            {
                _elementCache = null;
                _cursor.Dispose();
            }
        }

        
        public override void Reset()
        {
            _position = 0;
            _passedElements = 0;
            GetCache(true);
        }

        public override long Position()
        {
            return _passedElements + _position;
        }

        public override IEnumerable<dynamic> GetIterator(Stream fs, bool reset, Type targetType = null)
        {
            throw new NotImplementedException();
        }

        

        public override IEnumerable<T> GetIterator(Stream fs, bool reset)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<T> GetIterator(IAsyncCursorSource<BsonDocument> cursorSource, bool reset,Type targetType = null)
        {
            _finder = cursorSource;
//            if (_size == 0 || reset)
//            {
//                _size = _finder.Count();
//            } 
            GetCache(reset);
            if (_position == -1) _position = 0;
            //if (reset) cursor.Dispose();
            if (_elementCache!=null && _elementCache.Count() > _position)
            {
                while(_elementCache.Count>0)
                {
                    foreach (var element in _elementCache)
                    {
                        var deserialized = BsonSerializer.Deserialize<T>(element);
                        base.FormatFields(deserialized as ExpandoObject);
                        yield return deserialized;
                        Interlocked.Increment(ref _position);
                    }
                    GetCache(false); 
                }; 
                //var element = _elementCache[_position];
                //var output = BsonSerializer.Deserialize<ExpandoObject>(element);
                //return element;
            }
            else
            {
                //return null;
            }
        }

        private void GetCache(bool reset)
        {
            lock (_lock)
            {
                if (_cursor == null || reset)
                {
                    _cursor = _finder.ToCursor();
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToList() : new List<BsonDocument>();
                }
                if (_elementCache != null && _position >= _elementCache.Count())
                {
                    _elementCache?.Clear();
                    _elementCache = _cursor.MoveNext() ? _cursor.Current.ToList() : new List<BsonDocument>();
                    _passedElements += _position;
                    _position = 0;
                }
            }
        }


        public override IInputFormatter Clone()
        {
            var formatter = new BsonFormatter<T>();
            formatter._finder = _finder;
            formatter._cursor = null;
            return formatter;
        }
    }
}