using System;
using System.Collections.Generic;
using Peeralize.Service.Models;

namespace Peeralize.Service.Integration.Blocks
{
    public class DomainUserSessionCollection
    {
        public IList<DomainUserSession> Sessions { get; set; }
        public string UserId { get; set; }
        public DateTime Created { get; set; }

        public DomainUserSessionCollection()
        {
            Sessions = new List<DomainUserSession>();
        }
        public DomainUserSessionCollection(IList<DomainUserSession> sessions) : this()
        {
            this.Sessions = sessions;
        }

    }
}