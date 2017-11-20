using System;
using System.Collections.Generic;

namespace Netlyt.Service.Models
{
    public class Score<T>
    {
        public Score ValueScore { get; private set; }
        public T Value { get; set; }

        public Score(Score val)
        {
            ValueScore = val ?? new Score();

        }
          
    }
    public class Score : IComparer<Score>
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

        public double Average()
        {
            return Value / Math.Max(1,Count);
        }

        public int Compare(Score x, Score y)
        {
            return x.Average().CompareTo(y.Average());
        }
    }
}