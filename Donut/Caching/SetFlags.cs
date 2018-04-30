namespace Donut.Caching
{
    public class SetFlags
    {
        public bool IsSorted { get; set; }

        public SetFlags(bool sorted)
        {
            IsSorted = sorted;
        }
    }
}