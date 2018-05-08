namespace Donut.Data
{
    public interface IAggregateKey
    {
        string Arguments { get; set; }
        string Name { get; set; }
        DonutFunction Operation { get; set; }
    }
}