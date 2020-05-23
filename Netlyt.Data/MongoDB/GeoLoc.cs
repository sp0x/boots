using System;

namespace Netlyt.Data.MongoDB
{
    [Serializable()]
    public class GeoLoc
    {
        public Int32 id;
        public double LAT = 0d;
        public double LNG = 0d;
        public GeoLoc()
        {
        }
        public GeoLoc(double lt, double ln)
        {
            this.LAT = lt;
            this.LNG = ln;
        }
        public override string ToString()
        {
            return $"{LAT}, {LNG}";
        }
    }
}