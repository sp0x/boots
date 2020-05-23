namespace Netlyt.Data
{
    public class IndexKey
    {
        public string Name { get; private set; }
        public byte Order { get; private set; }

        public IndexKey(string name, byte order)
        {
            this.Name = name;
            this.Order = order;
        }
    }
}