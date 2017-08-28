using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
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