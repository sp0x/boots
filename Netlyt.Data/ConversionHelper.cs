using System.Linq;

namespace Netlyt.Data
{
    public class ConversionHelper
    {
        public static bool CBoolEx(params bool[] bools)
        {
            if (bools == null || bools.Length == 0)
                return false;
            return bools.All(x => x);
        }
    }
}