using System;

namespace Donut.Caching
{
    public class PropertySetter<TEntity, TValue> : IClrPropertySetter
        where TEntity : class
    {
        private readonly Action<TEntity, TValue> _setter;

        public PropertySetter(Action<TEntity, TValue> setter)
        {
            _setter = setter;
        }

        public virtual void SetClrValue(object instance, object value)
            => _setter((TEntity)instance, (TValue)value);
    }
}