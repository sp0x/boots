namespace Peeralize.Service.Integration.Blocks
{
    public class Score
    {
        public double Value { get; set; }
        public int Count { get; set; }

        public Score() : this(0)
        { 
        }
        public Score(double val)
        {
            Value = val;
            Count = 1;
        }

        public static Score operator ++(Score a)
        {
            a.Count++;
            return a;
        }

        public static Score operator +(Score a, double newScore)
        {
            a.Value += newScore;
            a.Count++;
            return a;
        }
         
    }
}