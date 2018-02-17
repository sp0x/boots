namespace Netlyt.Service.Integration.Import
{
    public class DestinationCollection
    {
        public string OutputCollection { get; private set; }
        public string ReducedOutputCollection { get; private set; }

        public DestinationCollection(string output, string reducedOutput)
        {
            OutputCollection = output;
            ReducedOutputCollection = reducedOutput;
        }
    }
}