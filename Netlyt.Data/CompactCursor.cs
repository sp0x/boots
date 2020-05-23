using System.Collections.Generic;
using System.Linq;

namespace Netlyt.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CompactCursor<T> 
        : Cursor<T> where T : ICompactModel
    {
        protected CompactCursor() : base()
        {
        }
        protected CompactCursor(IEnumerable<T> data) : base(data)
        {
        }
        /// <summary>
        /// Create a new cursor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public new static CompactCursor<T> Create(IEnumerable<T> data, int index)
        {
            return new CompactCursor<T>(data) { Index = index };
        }
        /// <summary>
        /// Returns an object with { index = index, items = itemCollection }
        /// </summary>
        /// <returns></returns>
        public new object ToJson()
        {
            var res = new
            {
                index = this.Index,
                items = ((IEnumerable<T>)this).Select(x=>x.Representation()).ToList()
            };
            return res;
        }
    }
}