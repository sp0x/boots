namespace Netlyt.Interfaces
{
    public interface IAggregateKey
    {
        string Arguments { get; set; }
        string Name { get; set; }
        IDonutFunction Operation { get; set; }
    }
}