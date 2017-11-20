using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class MysqlSource : InputSource
    {
        public MysqlSource(IInputFormatter formatter) : base(formatter)
        {
        }
        public override IIntegrationTypeDefinition GetTypeDefinition()
        {
            throw new System.NotImplementedException();
        }

        public override dynamic GetNext()
        {
            throw new System.NotImplementedException();
        }

        public override void DoDispose()
        {
        }
    }
}