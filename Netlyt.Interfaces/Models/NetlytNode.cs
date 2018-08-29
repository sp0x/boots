namespace Netlyt.Interfaces.Models
{
    public class NetlytNode
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        public virtual Organization Organization { get; set; }
        public NodeRole Role { get; set; } = NodeRole.Slave;
        public bool Active { get; set; } = true;
        public static NetlytNode Cloud { get; private set; } = new NetlytNode("Cloud") { Role = NodeRole.Cloud };

        public const string NODE_TYPE_CLOUD = "cloud";
        public const string NODE_TYPE_ON_PREM = "prem";

        public NetlytNode()
        {

        }

        public override string ToString()
        {
            var orgName = Organization == null ? "NoOrg" : Organization.Name;
            return $"{Name} - {orgName}@{Address} [{Role.ToString()}]";
        }

        public bool IsCloud()
        {
            return this.Equals(Cloud);
        }

        public NetlytNode(string name) : this()
        {
            this.Name = name;
        }

        public bool HasToVerifyLogins()
        {
            return !this.Equals(Cloud); //Only our cloud nodes dont have to verify logins.
        }
    }

    public enum NodeRole { Master, Slave, Cloud }
}
