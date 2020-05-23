using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Netlyt.Data
{
    /// <summary>
    /// Represents a basic blueprint of what a Database Basic object (SqlList, MongoList) can do.
    /// </summary>
    /// <typeparam name="TRecord">The type of the handled data.</typeparam>
    /// <remarks></remarks>
    public abstract class DBBase<TRecord> 
        : IDbListBase<TRecord>
        where TRecord : class
    {
        static DBBase()
        {
            //DBConfig.LoadConfiguration();
        }

        #region "Abstract methods"


        public abstract bool Connected { get; }

        public abstract string CollectionName { get; }

        public abstract int Size { get; }

        public abstract IQueryable<TRecord> AsQueryable1 { get; }
        

        public abstract IQueryable AsQueryable { get; }

        //        public abstract void Save(Entity element);
        public abstract void Save(TRecord element);
        public abstract void Add(TRecord element);
        public abstract void AddRange(IEnumerable<TRecord> element);


//        public abstract void Save<TXRecord>(TRecord elem)
//            where TXRecord : TRecord;
        public abstract List<Index> GetIndexes();

        public void Save(object element)
        {
            Save((TRecord)element);
        }

        public void Add(object element)
        {
            Add((TRecord) element);
        }
        public void AddRange(IEnumerable<object> elements)
        {
            AddRange(elements.Cast<TRecord>());
        }
        //        public abstract void Save<TXRecord>(Entity element)
        //            where TXRecord : Entity;

        //Public Function Save(Of TRecord1 As IdAble)(element As TRecord1) As Boolean Implements IDbListBase.Save
        //    Return SaveElement(CObj(element))
        //End Function
        //Public Function Exists(Of TRecord1)(ByVal element As TRecord1, Optional ByRef value As TRecord1 = Nothing) As Boolean Implements IDbListBase.Exists
        //    Return ExistsElement(CObj(element), CObj(value))
        //End Function
        //Public Function Save(Of TRecord1 As IdAble)(ByVal elements As IEnumerable(Of TRecord1)) As Boolean Implements IDbListBase.Save
        //    Return Save(CObj(elements))
        //End Function
        //Public Function SaveOrUpdate(Of TRecord1 As IdAble)(ByVal element As TRecord1) As Boolean Implements IDbListBase.SaveOrUpdate
        //    Return SaveOrUpdate(CObj(element))
        //End Function
        //Public Function SaveOrUpdate(Of TRecord1 As IdAble)(ByVal element As IEnumerable(Of TRecord1)) As Boolean Implements IDbListBase.SaveOrUpdate
        //    Throw New NotImplementedException()
        //End Function


        public abstract bool Save(IEnumerable<object> elements);
        public abstract bool Save(IEnumerable elements); 
        public abstract bool SaveOrUpdate(TRecord element);
        public abstract bool SaveOrUpdate<TMember>(TRecord existingDomain, Expression<Func<TRecord, TMember>> memberSelector, TMember value);
        public abstract bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith);
        public abstract IEnumerable<TRecord> Range(int skip, int limit);

        public bool SaveOrUpdate(object element)
        {
            return SaveOrUpdate((TRecord) element);
        }

        //public abstract bool SaveOrUpdate(IEnumerable<Entity> element);
        public abstract bool SaveOrUpdate(IEnumerable<TRecord> element);

        public bool SaveOrUpdate(IEnumerable<object> element)
        {
            return SaveOrUpdate((IEnumerable<TRecord>)element);
        }

        public abstract bool Delete(TRecord elem);
        public abstract bool DeleteAll(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null);

        public abstract Task<bool> DeleteAllAsync(IEnumerable<TRecord> elements,
            CancellationToken? cancellationToken = null);

        public abstract bool Trash();
        
        public abstract IEnumerator<TT> GetEnumerator<TT>() where TT : class;
        public abstract IEnumerable<TT> Enumerable<TT>() where TT : class;
        public abstract IEnumerable<TRecord> Where(Expression<Func<TRecord, bool>> predicate, int count, int skip);
        public abstract TRecord FirstOrDefault(Expression<Func<TRecord, bool>> predicate);
        public abstract TRecord First(Expression<Func<TRecord, bool>> predicate);
        public abstract bool Any(Expression<Func<TRecord, bool>> predicate);

        public abstract bool Exists(TRecord target, ref TRecord doc);
        public abstract bool Exists(object target, ref object doc);
        //public abstract new bool Exists(Entity element, ref TypedEntity value);
        //public abstract new bool Exists(Func<Entity, bool> predicate, ref Entity doc);
        public abstract bool Exists(Expression<Func<TRecord, bool>> predicate, TRecord doc);
        #endregion
        

        #region "Generic methods" 

        public abstract IEnumerable<TRecord> In<TMember>(Expression<Func<TRecord, TMember>> func,
            IEnumerable<TMember> values)
            where TMember : class;

        public abstract IEnumerable<TRecord> Where(DataQuery predicate);

        #endregion


        public IEnumerator<TRecord> GetEnumerator()
        {
            return this.AsQueryable1.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}