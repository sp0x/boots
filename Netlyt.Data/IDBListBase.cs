using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Netlyt.Data
{

    //    /// <summary>
    //    /// IDbObject, controls the objects of type "TT" in the selected database.
    //    /// </summary>
    //    /// <typeparam name="TT"></typeparam>
    //    /// <remarks></remarks>
    //    public interface IDbObject<TT> : IDbObject where TT : class
    //    {
    //        TT Get(Expression<Func<TT, bool>> expression);
    //        IQueryOver<TT> GetAll();
    //        IQueryOver<TT> GetAll(Expression<Func<TT, bool>> expression);
    //        IQueryOver<TT> Find(Expression<Func<TT, bool>> expression);
    //        TT Save(TT model);
    //        void SaveAll(TT[] models);
    //        void Delete(int id);
    //    }


    /// <summary>
    /// A generic interface linking all types of generic DataBase List abstractions.
    /// </summary>
    /// <remarks></remarks>
    public interface IDbListBase : IEnumerable
    {
        bool Connected { get; }
        string CollectionName { get; }
        int Size { get; }
        IQueryable AsQueryable { get; }
        /// <summary>
        /// Returns the size of the collection/CollectionName
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>


        //IEnumerable<TRecord> Get<TRecord>(Func<TRecord, bool> predicate);

        IEnumerator<T> GetEnumerator<T>() where T : class;

        bool Exists(object element, ref object value);
        void Save(object element); 
        bool Save(IEnumerable<object> elements);
        void Add(object element);
        void AddRange(IEnumerable<object> elements);
        /// <summary>
        /// Saves an enumeration of elements. Each of them MUST be based off of DbBlob.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        bool Save(IEnumerable elements);
        bool SaveOrUpdate(object element);
        bool SaveOrUpdate(IEnumerable<object> element);

#if DEBUG
        /// <summary>
        /// Remove all elements from the source
        /// </summary>
        Boolean Trash();
#endif

        


    }
}