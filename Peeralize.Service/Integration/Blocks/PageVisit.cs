using System;

namespace Peeralize.Service.Integration.Blocks
{
    public class PageVisit
    {
        public TimeSpan Duration { get; private set; }
        public int Transitions { get; set; }

        public PageVisit(TimeSpan dur)
        {
            Duration = dur;
            Transitions = 1;
        }

        public void Add(TimeSpan ts)
        {
            Duration += ts;
            Transitions++;
        }
    }
}