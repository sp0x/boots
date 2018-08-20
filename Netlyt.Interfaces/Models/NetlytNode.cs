namespace Netlyt.Interfaces.Models
{
    public class NetlytNode
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        public virtual Organization Organization { get; set; }
        public NodeRole Role { get; set; }
        public bool Active { get; set; } = true;
        public static NetlytNode Cloud { get; private set; } = new NetlytNode("Cloud");

        public const string NODE_TYPE_CLOUD = "cloud";
        public const string NODE_TYPE_ON_PREM = "prem";

        public NetlytNode()
        {

        }

        public NetlytNode(string name) : this()
        {
            this.Name = name;
        }
    }

    public enum NodeRole { Master, Slave, Cloud }
}
