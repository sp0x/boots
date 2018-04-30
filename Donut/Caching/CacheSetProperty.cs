using System;
using System.Collections.Generic;

namespace Donut.Caching
{
    public class CacheSetProperty
    {
        public string Name { get; set; }

        public Type ClrType { get; set; }
        public IClrPropertySetter Setter { get; set; }
        public IEnumerable<Attribute> Attributes{ get; set; }

        public CacheSetProperty(string name, Type type, IClrPropertySetter setter)
        {
            Name = name;
            ClrType = type;
            Setter = setter;
        } 
    }
}