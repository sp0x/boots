using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut.IntegrationSource
{
    public class MysqlSource : Donut.IntegrationSource.InputSource
    {
        public MysqlSource() : base()
        {
        }
        public override IIntegration ResolveIntegrationDefinition()
        {
            throw new System.NotImplementedException();
        }
        public override IEnumerable<T> GetIterator<T>()
        {
            return GetIterator(typeof(T)).Cast<T>();
        }
        public override IEnumerable<dynamic> GetIterator(Type targetType = null)
        {
            throw new NotImplementedException();
        }

        public override void DoDispose()
        {
        }
    }
}