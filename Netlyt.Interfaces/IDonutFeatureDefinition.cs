namespace Netlyt.Interfaces
{
    public interface IDonutFeatureDefinition
    {
        string GetValue();
    }

    public interface IDonutCodeFeatureDefinition : IDonutFeatureDefinition
    {
        string ExtractionScript { get; set; }
        string PrepareScript { get; set; }
    }
}