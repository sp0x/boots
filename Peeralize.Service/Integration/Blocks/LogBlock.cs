using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Integration.Blocks
{
    public class Log : IntegrationBlock
    {
        public Log(string userId)
        {
            this.UserId = userId;
        }
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            Console.WriteLine(intDoc.Document.ToString());
            return intDoc;
        }
    }
}
