using System.Linq;
using System.Reflection;
using Donut;

namespace nvoid.db.Caching
{
    public abstract class AccessorFactory<TAccessor>
        where TAccessor : class
    {
        private static readonly MethodInfo _genericCreate
            = typeof(AccessorFactory<TAccessor>).GetTypeInfo().GetDeclaredMethods(nameof(CreateGeneric)).Single();

        public TAccessor Create(PropertyInfo propertyInfo)
        {
            var boundMethod = _genericCreate.MakeGenericMethod(
                propertyInfo.DeclaringType,
                propertyInfo.PropertyType,
                propertyInfo.PropertyType.UnwrapNullableType());
            try
            {
                return (TAccessor)boundMethod.Invoke(this, new object[] { propertyInfo });
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }
        }
        protected abstract TAccessor CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(
            PropertyInfo propertyInfo)
            where TEntity : class;
    }
}