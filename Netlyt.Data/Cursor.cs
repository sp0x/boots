using System.Collections.Generic;

namespace Netlyt.Data
{
    /// <summary>   A cursor abstraction for queries.
    ///             TODO: Make this a collection instead of list. </summary>
    ///
    /// <remarks>   Vasko, 14-Dec-17. </remarks>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>

    public class Cursor<T> : List<T>
    { 
        public int Index { get; set; }

        #region Constructors
        protected Cursor()
        {
        }
        protected Cursor(IEnumerable<T> items) : base(items) 
        {
        }
        #endregion

        #region Methods

        #region Statics
        /// <summary>
        /// Create a new cursor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cursor<T> Create(IEnumerable<T> data, int index)
        {
            return new Cursor<T>(data) {Index = index};
        }
        #endregion

        /// <summary>
        /// Returns a compact representation of a standart cursor, beware that you should manage the type casts.
        /// </summary>
        /// <typeparam name="TCompactable"></typeparam>
        /// <returns></returns>
        public CompactCursor<TCompactable> Compact<TCompactable>() where TCompactable : ICompactModel
        { 
            return CompactCursor < TCompactable >.Create( this as IEnumerable<TCompactable>, Index);
        }

        public object ToJson()
        {
            var res = new
            { 
                index = this.Index,
                items = this
            };
            return res;
        }
        #endregion

    }
}