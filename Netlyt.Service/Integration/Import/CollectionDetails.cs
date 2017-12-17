namespace Netlyt.Service.Integration.Import
{
    public class CollectionDetails
    {
        public string OutputCollection { get; private set; }
        public string ReducedOutputCollection { get; private set; }

        public CollectionDetails(string output, string reducedOutput)
        {
            OutputCollection = output;
            ReducedOutputCollection = reducedOutput;
        }
    }
}