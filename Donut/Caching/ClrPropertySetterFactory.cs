using System;
using System.Linq.Expressions;
using System.Reflection;
using nvoid.db.Caching;

namespace Donut.Caching
{
    public class ClrPropertySetterFactory : AccessorFactory<IClrPropertySetter>
    {
        protected override IClrPropertySetter CreateGeneric<TEntity, TValue, TNonNullableEnumValue>(
            PropertyInfo propertyInfo)
        {
            var memberInfo = propertyInfo.FindGetterProperty();

            if (memberInfo == null)
            {
                throw new InvalidOperationException($"No setter for property: {memberInfo.Name}");
            }
            var entityParameter = Expression.Parameter(typeof(TEntity), "entity");
            var valueParameter = Expression.Parameter(typeof(TValue), "value");

            var expression = Expression.Lambda<Action<TEntity, TValue>>(
                Expression.Assign(
                    Expression.MakeMemberAccess(entityParameter, memberInfo),
                    valueParameter),
                entityParameter,
                valueParameter);
            var setter = expression.Compile();
            var setterWrap = new PropertySetter<TEntity, TValue>(setter);
            return setterWrap;
        }
    }
}