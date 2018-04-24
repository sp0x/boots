using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Interfaces;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class MysqlSource : InputSource
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