using System;

namespace Netlyt.ServiceTests.Netinfo
{
    public class DomainUserSession
    {
        public DomainUserSession()
        {

        }
        public DomainUserSession(string lastDomain, DateTime visited, TimeSpan visitDuration)
        {
            this.Domain = lastDomain;
            this.Visited = visited;
            this.Duration = visitDuration;
        }
        public TimeSpan Duration { get; private set; }
        public string Domain { get; private set; }
        public DateTime Visited { get; private set; }
    }
}