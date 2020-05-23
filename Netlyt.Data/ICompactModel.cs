namespace Netlyt.Data
{
    /// <summary>
    /// A model that has a compact representation, commonly used to provide information over a JSON/SOAP Interface
    /// </summary>
    public interface ICompactModel
    {
        object Representation();
    }
}