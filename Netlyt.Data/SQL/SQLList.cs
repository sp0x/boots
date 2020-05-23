using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NHibernate;
using NHibernate.Criterion;

namespace Netlyt.Data.SQL
{
    /// <summary>
    /// An object that can directly interact with the database, or query specific objects only without type specification.
    /// </summary>
    /// <typeparam name="TRecord">The type of the object/CollectionName</typeparam>
    /// <remarks></remarks>
    public class SQLList<TRecord>
        : DBBase<TRecord>, IDisposable where TRecord : class
    {
        private string _connectionString = "";
        private ISession mSession;
        protected internal ISessionFactory _sessionFactory { get; set; }
        //Private _client As SqlCli 
        private readonly object @lock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISession Session
        {
            get { return mSession; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected ISessionFactory SessionFactory
        {
            get
            {
                return _sessionFactory;
            }
        }

        public override bool Connected
        {
            get
            {
                if (Session == null) return false;
                return Session.IsConnected;
            }
        }

        public override string CollectionName
        {
            get
            {
                return Session.GetEntityName(default(TRecord));
            }
        }

        public override int Size
        {
            get
            {
                return (int)GetSize();
            }
        }

        public override IQueryable AsQueryable
        {
            get
            {
                return Session.Query<TRecord>();
            }
        }
        public override IQueryable<TRecord> AsQueryable1
        {
            get
            {
                return Session.Query<TRecord>();
            }
        }

        #region Construction
        public SQLList(MappingType mappingType = MappingType.NHibernate, bool underDebug = false, string db = null, string endpoint = null, string user = null, string password = null, Action<string> onError = null)
        {
            _sessionFactory = DbSessionFactory.Open(false, mappingType, underDebug, db, endpoint, user, password, onError);
        }
        public SQLList(string db)
        {
            _connectionString = db;
            _sessionFactory = DbSessionFactory.Open(db);
        }
        public SQLList(ISessionFactory sessionFactory)
        {
            this._sessionFactory = sessionFactory;
        }
        //        public SQLList(ISession session, IEnumerable<TRecord> data, FlushMode flushMode = FlushMode.Auto)
        //        {
        //            AddRange(data);
        //            mSession = session;
        //            mSession.FlushMode = flushMode;
        //        }
        public SQLList(bool recreate, MappingType mappingType = MappingType.NHibernate, bool underDebug = false)
        {
            this._sessionFactory = DbSessionFactory.Open(recreate, mappingType, underDebug);
        }
        #endregion

        protected void OpenSession(FlushMode flushMode = FlushMode.Auto)
        {
            if (mSession == null) return;
            mSession = SessionFactory.OpenSession();
            mSession.FlushMode = flushMode;
        }

        public void BeginTransaction()
        {
            Session.BeginTransaction();
        }
        public void EndTransaction()
        {
            if (Session.Transaction != null)
            {
                Session.Transaction.Commit();
            }
        }
        //        /// <summary>
        //        /// Fills the list with all the rows from the SqlDb.
        //        /// </summary>
        //        /// <remarks></remarks>
        //        public void FetchAll()
        //        {
        //            if (Session == null) throw new SessionNotInitializedException();
        //            IList<TRecord> items = GetAll(Session);
        //            AddRange(items);
        //        }
        //        /// <summary>
        //        /// Clears the list and fills it with all the rows from the SqlDb.
        //        /// </summary>
        //        /// <remarks></remarks>
        //        public void ClearAndFetchAll()
        //        {
        //            if (Session == null) throw new SessionNotInitializedException();
        //            IList<TRecord> items = GetAll(Session);
        //            base.Trash();
        //            base.AddRange(items);
        //        }
        //        /// <summary>
        //        /// Gets all the rows from the SqlDb. DOES NOT SAVE THEM!
        //        /// </summary>
        //        /// <remarks></remarks>
        //        public SQLList<TRecord> Rows()
        //        {
        //            if (Session == null) throw new SessionNotInitializedException();
        //            return new SQLList<TRecord>(Session);
        //        }
        //         
        //        /// <summary>
        //        /// Gets an item by it's index (NON SqlDb RELATIONAL)
        //        /// </summary>
        //        /// <param name="key">The index</param>
        //        /// <value></value>
        //        /// <returns></returns>
        //        /// <remarks></remarks>
        //        public TRecord GetListItem(int key)
        //        {
        //            return base[key];
        //        }

        /// <summary>
        /// Gets every element in the database.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public virtual IQueryOver<TRecord> GetAll()
        {
            lock (@lock)
            {
                return Session.QueryOver<TRecord>();
            }
        }
        /// <summary>
        /// Gets every element in the database, after the offset, using a special Size.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public virtual IEnumerable<TRecord> GetAll(Int32 offset, Int32 size)
        {
            lock (@lock)
            {
                IEnumerable<TRecord> items = null;
                try
                {
                    items = Session.QueryOver<TRecord>().Skip(offset).Take(size).List();
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    string iexp = "";
                    if (ex.InnerException != null)
                        iexp = ex.InnerException.Message;
                }
                return items;
            }
        }
        public virtual IQueryOver<TRecord> GetAllWhere(Expression<Func<TRecord, bool>> expression, Int32 offset, Int32 size)
        {
            lock (@lock)
            {
                IQueryOver<TRecord> iqr = Session.QueryOver<TRecord>();
                return Session.QueryOver<TRecord>().Where(expression).Skip(offset).Take(size);
                // Return items.List() '(Me.Session)
            }
        }
        public virtual IQueryOver<TRecord> GetAllWhere(Expression<Func<TRecord, bool>> expression)
        {
            lock (@lock)
            {
                return Session.QueryOver<TRecord>().Where(expression);
                //  Return items
            }
        }
        IQueryOver<TRecord> GetAll(Expression<Func<TRecord, bool>> expression)
        {
            return GetAllWhere(expression);
        }

        public TRecord Get(Expression<Func<TRecord, Boolean>> expression)
        {
            lock (this)
            {
                return (TRecord)Session.QueryOver<TRecord>().Where(expression).Take(1);
            };
        }

        public override bool Exists(Expression<Func<TRecord, bool>> predicate, TRecord doc)
        {
            throw new NotImplementedException();
        }

        public IEnumerable Get(Func<Delegate> predicateFactory)
        {
            var crit = from x in Session.QueryOver<TRecord>()
                       where (bool)predicateFactory().DynamicInvoke(x)
                       select x;
            return crit.List();
        }

        #region "External helpers"
        /// <summary>
        /// Gets all the rows from the CollectionName.
        /// </summary>
        /// <param name="sess">The session which we're gonna use to get the data from.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IList<TRecord> GetAll(ISession sess = null)
        {
            bool created = true;
            //if (sess == null) { sess = DBSessionFactory.Open() ; created = true; }
            if (sess == null)
                throw new SessionNotInitializedException();
            IList<TRecord> items = sess.QueryOver<TRecord>().List();
            if (created)
                sess.Close();
            return items ?? new List<TRecord>();
        }
        //        /// <summary>
        //        /// Gets a list to the defined CollectionName, creates a session and also fills-in all the rows.
        //        /// </summary>
        //        /// <returns>A newly created SQLList containing all items and a new session.</returns>
        //        /// <remarks></remarks>
        //        public static SQLList<TRecord> GetList()
        //        {
        //            ISession sess = DbSessionFactory.Create().OpenSession();
        //            if (sess == null)
        //                throw new SessionNotInitializedException();
        //            List<TRecord> items = (List<TRecord>)sess.QueryOver<TRecord>().List();
        //            items = items ?? new List<TRecord>();
        //            return new SQLList<TRecord>(sess, items);
        //        }
        #endregion

        /// <summary>
        /// Gets items by their offset and limit them to a specific Size.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The Size of elements to get.</param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public IEnumerable<TRecord> GetItems(int offset, long size)
        {
            if (size == 0) size = (this.GetSize() - offset);
            if (GetSize() < offset | this.GetSize() < offset + size)
            {
                throw new IndexOutOfRangeException("Invalid offset or Size, check list.Size!");
            }
            return GetAll(offset, (int)size);
        }
        //        /// <summary>
        //        /// Gets an item from the database which answers to the defined conditions.
        //        /// </summary>
        //        /// <param name="predicate">Conditions</param>
        //        /// <value></value>
        //        /// <returns></returns>
        //        /// <remarks></remarks>
        //        public SQLList<TRecord> this[Expression<Func<TRecord, bool>> predicate]
        //        {
        //            get
        //            {
        //                try
        //                {
        //                    IList<TRecord> @out = GetAllWhere(predicate).List();
        //                    return OutOfMemoryException;
        //                }
        //                catch (Exception ex)
        //                { 
        //                    return null;
        //                }
        //            }
        //            set { throw new NotImplementedException(); }
        //        }
        public IEnumerable<TRecord> GetRange(int offset, int size)
        {
            return GetAll(offset, size);
        }

        #region "Fetch by Id"
        /// <summary>
        /// Gets an item by it's id, from the SqlDb.
        /// </summary>
        /// <param name="id"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public object GetById(Object id)
        {
            if (Session == null) throw new SessionNotInitializedException();
            return Session.Get<TRecord>(id);
        }
        /// <summary>
        /// Gets an item by it's id, from the SqlDb.
        /// </summary>
        /// <param name="id"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public TRecord GetById(string id)
        {
            lock (@lock)
            {
                if (Session == null) throw new SessionNotInitializedException();
                return Session.Get<TRecord>(id);
            }
            //set { Connection.SaveOrUpdate(value); }
        }

        public void SetById(int id, TRecord value)
        {
            Session.SaveOrUpdate(nameof(TRecord), value, id);
        }
        #endregion


        //    Public Overrides Function [Get](id As Integer) As t
        //        Return Session.QueryOver(Of t)().Where(Function(x) Me.id = id).List().FirstOrDefault()
        //    End Function
        public virtual IQueryOver<TRecord> Find(Expression<Func<TRecord, bool>> xp)
        {
            lock (@lock)
            {
                return Session.QueryOver<TRecord>().Where(xp);
            }
        }

        public override void Save(TRecord model)
        {
            if (model != null)
            {
                try
                {
                    mSession.SaveOrUpdate(model);
                    mSession.Flush();
                }
                catch (Exception ex)
                {

                    Trace.WriteLine(ex.Message);
                    if (null != (ex.InnerException))
                        Trace.WriteLine(ex.InnerException.Message);
                }
            }
        }

        public override void Add(TRecord model)
        {
            if (model != null)
            {
                try
                {
                    mSession.Save(model);
                    mSession.Flush();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    if (null != (ex.InnerException))
                        Trace.WriteLine(ex.InnerException.Message);
                }
            }
        }

        public override void AddRange(IEnumerable<TRecord> model)
        {
            if (model != null)
            {
                using (var t = mSession.BeginTransaction())
                {
                    foreach (var element in model)
                    {
                        mSession.Save(element);
                    }
                    t.Commit();
                    mSession.Flush();
                }
            }
        }

        /// <summary>   Gets the indexes.
        ///             TODO: Add db index enumeration </summary>
        ///
        /// <remarks>   Vasko, 15-Dec-17. </remarks>
        ///
        /// <returns>   The indexes. </returns>

        public override List<Index> GetIndexes()
        {
            var idIndex = new Index("Id", new List<IndexKey>() { new IndexKey("Id", (byte)1) }, true);
            var indexes = new List<Index>() { idIndex };
            return indexes;
        }

        //        public override void Save<TXRecord>(Entity elem)
        //        {
        //            Save((TRecord) elem);
        //        }
        //        
        public virtual void SaveAll(IEnumerable<TRecord> models)
        {
            if (models != null)
            {
                using (ITransaction tx = Session.BeginTransaction())
                {
                    try
                    {
                        foreach (TRecord tObj in models)
                        {
                            mSession.SaveOrUpdate(tObj);
                        }
                        tx.Commit();
                    }
                    catch
                    {

                    }
                }
            }
            Session.Flush();
        }

        public virtual void Dispose()
        {
            if (Session != null)
                Session.Dispose();
        }


        public override bool Save(IEnumerable<object> elements)
        {
            lock (@lock)
            {
                foreach (var element in elements)
                {
                    Session.Save(element);
                }
            }
            return true;
        }

        public override bool Save(IEnumerable elements)
        {
            lock (@lock)
            {
                foreach (var element in elements)
                {
                    Session.Save(element);
                }
            }
            return true;
        }


        public override bool SaveOrUpdate(IEnumerable<TRecord> element)
        {
            Session.SaveOrUpdate(element);
            return true;
        }

        #region Deletion
        public override bool Delete(TRecord elem)
        {
            Session.Delete(elem);
            return true;
        }

        public virtual void Delete(String id)
        {
            lock (@lock)
            {
                var entityQuery = DbQueryProvider.GetInstance().GetMemberQuery<TRecord>(id, "id");
                TRecord model = (TRecord)Session.QueryOver<TRecord>().Where(entityQuery);
                if (model != null)
                {
                    Session.Delete(model);
                }
                Session.Flush();
            }
        }

        public override bool DeleteAll(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null)
        {
            return DeleteAllAsync(elements, cancellationToken).Result;
        }

        public override Task<bool> DeleteAllAsync(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null)
        {
            lock (@lock)
            {
                Func<bool> runQuery = new Func<bool>(() =>
                {
                    string strQuery = SqlQueryHelper.WhereEntities(elements);
                    int delCount = Session.Delete(strQuery);
                    Session.Flush();
                    return true;
                });
                return Task<bool>.Run(runQuery);

            }
        }
        #endregion

        public override bool Trash()
        {
            return ClearTable();
        }

        #region "Clearing"
        /// <summary>
        /// Clears the CollectionName's contents
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public static bool ClearTable(string table)
        {
            using (ISession sess = DbSessionFactory.Create().OpenSession())
            {
                using (ITransaction trans = sess.BeginTransaction())
                {
                    Int32 up = sess.CreateSQLQuery("TRUNCATE " + table).ExecuteUpdate();
                    trans.Commit();
                }
            }
            return true;
        }
        public bool ClearTable()
        {
            if (Session == null)
                throw new SessionNotInitializedException();
            using (ITransaction trans = Session.BeginTransaction())
            {
                try
                {
                    Int32 up = Session.CreateSQLQuery("TRUNCATE " + CollectionName).ExecuteUpdate();
                    trans.Commit();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        #endregion


        public override IEnumerator<TT1> GetEnumerator<TT1>()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TT> Enumerable<TT>()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TRecord> Where(Expression<Func<TRecord, bool>> predicate, int count = 0, int skip = 0)
        {
            var queryOver = Session.QueryOver<TRecord>().Where(predicate);
            if (count > 0) queryOver.Take(count);
            if (skip > 0) queryOver.Skip(skip);
            return queryOver.List<TRecord>();
        }

        public override IEnumerable<TRecord> In<TMember>(Expression<Func<TRecord, TMember>> func, IEnumerable<TMember> values)
        {
            var modifiedExpression = ExpressionTransformer<TMember, object>.Transform<TRecord>(func);
            var queryOver = Session.QueryOver<TRecord>()
                .Where(Restrictions.On<TRecord>(modifiedExpression)
                    .IsIn(values.Cast<object>().ToArray()));
            return queryOver.List<TRecord>();
        }




        public override TRecord FirstOrDefault(Expression<Func<TRecord, bool>> predicate)
        {
            var queryOver = Session.QueryOver<TRecord>().Where(predicate).Take(1);
            return queryOver.SingleOrDefault<TRecord>();
        }

        public override TRecord First(Expression<Func<TRecord, bool>> predicate)
        {
            return FirstOrDefault(predicate);
        }

        public override bool Any(Expression<Func<TRecord, bool>> predicate)
        {
            return Session.QueryOver<TRecord>().Where(predicate).RowCount() > 0;
        }

        public override bool Exists(TRecord target, ref TRecord doc)
        {
            throw new NotImplementedException();
        }

        public override bool Exists(object target, ref object doc)
        {
            throw new NotImplementedException();
        }

        //        public override bool Exists(Entity element, ref TypedEntity value)
        //        {
        //            throw new NotImplementedException();
        //        }

        #region "Count"
        /// <summary>
        /// Size of rows answering the condition, in the SqlDb
        /// </summary>
        /// <param name="exp"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public long GetSize(Expression<Func<bool>> exp)
        {
            return Session.QueryOver<TRecord>().Where(exp).RowCountInt64();
        }

        /// <summary>
        /// Size of all rows
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public long GetSize()
        {
            return Session.QueryOver<TRecord>().RowCountInt64();
        }

        public override bool SaveOrUpdate(TRecord element)
        {
            var y = Session.Query<TRecord>().Where(x => x == element).FirstOrDefault();
            Session.Update(element);
            return true;
        }

        public override bool SaveOrUpdate<TMember>(TRecord element, Expression<Func<TRecord, TMember>> memberSelector, TMember value)
        {
            //var existingElement = Session.QueryOver<TRecord>().Where(x => memberSelector.Compile().Invoke(x).Equals(value));
            Session.SaveOrUpdate(element);
            return false;
        }

        public override bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith)
        {
            var document = Session.Query<TRecord>().Where(predicate).FirstOrDefault();
            if (document == null)
            {
                Session.Save(replaceWith);
            }
            else Session.Update(replaceWith);
            return true;
        }

        public override IEnumerable<TRecord> Range(int skip, int limit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The list's item Size.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public Int32 Count
        {
            get { return 0; }
        }

        public override IEnumerable<TRecord> Where(DataQuery predicate)
        {
            throw new NotImplementedException();
        }

        #endregion

    }

     public static class ReflectionExtensions
     {
         public static string GetPropertyName<TEntity, TProperty>(this Type entityType, Expression<Func<TEntity, TProperty>> propertyExpression)
         {
             return propertyExpression.Name;
         }
     }
}