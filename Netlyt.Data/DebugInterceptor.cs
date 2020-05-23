using System.Diagnostics;
using NHibernate;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace Netlyt.Data
{
    internal class DebugInterceptor : EmptyInterceptor
    {
        public ISessionFactory SessionFactory { get; set; }

        public override object Instantiate(string clazz, object id)
        {

            System.Type type = System.Type.GetType(clazz);
            if ((type != null))
            {
                dynamic instance = null;
                SessionFactory.GetClassMetadata(clazz).SetIdentifier(instance, id);
                return instance;
            }

            return base.Instantiate(clazz, id);
        }

        public override string GetEntityName(object entity)
        {
            Trace.WriteLine(entity.ToString());
            return base.GetEntityName(entity);
        }

        public override void OnCollectionUpdate(object collection, object key)
        {
            base.OnCollectionUpdate(collection, key);
        }

        public override bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            dynamic load = base.OnLoad(entity, id, state, propertyNames, types);
            Trace.WriteLine(entity);
            return load;
        }

        //Public Overrides Function GetEntityName(ByVal entity As Object) As String
        //    Dim entityName = MyBase.GetEntityName(entity)
        //    Trace.WriteLine(entity)
        //    Return entityName
        //End Function

        public override object GetEntity(string entityName, object id)
        {
            var entity = base.GetEntity(entityName, id);
            if (entity != null) Trace.WriteLine(entity.ToString());
            return entity;
        }

        public override SqlString OnPrepareStatement(SqlString sql)
        {
            dynamic ret = base.OnPrepareStatement(sql);
            Trace.WriteLine(sql.ToString());
            return ret;
        }
    }
}