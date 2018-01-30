using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public class InternalCacheSet<T> :
        CacheSet<T>, IQueryable<T> where T : class
    {
        private ICacheSetCollection _context;
        public InternalCacheSet([NotNull] ICacheSetCollection context)
        {
            _context = context;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType { get; }
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }
    }
}