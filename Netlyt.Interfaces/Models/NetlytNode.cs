namespace Netlyt.Interfaces.Models
{
    public class NetlytNode
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Organization Organization { get; set; }
        public NodeRole Role { get; set; }
        public bool Active { get; set; } = true;
    }

    public enum NodeRole { Master, Slave }
}
