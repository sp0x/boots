using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class MysqlSource : InputSource
    {
        public MysqlSource(IInputFormatter formatter) : base(formatter)
        {
        }
        public override IIntegration ResolveIntegrationDefinition()
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<dynamic> GetIterator()
        {
            throw new NotImplementedException();
        }

        public override void DoDispose()
        {
        }
    }
}