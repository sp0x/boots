using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Integration.Blocks
{
    public class Log : IntegrationBlock
    {
        private Action<IntegratedDocument> _logger;
        public Log(string userId, Action<IntegratedDocument> logger)
            :base(capacity: 100000, processingType: ProcessingType.Action)
        {
            this.UserId = userId;
            _logger = logger;
        }
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            _logger(intDoc);
            return intDoc;
        }
    }
}
