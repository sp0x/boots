namespace Netlyt.Interfaces
{
    public interface IHarvesterResult
    {
        int ProcessedEntries { get; }
        int ProcessedShards { get; }
    }
}