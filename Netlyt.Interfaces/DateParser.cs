using System;
using System.Globalization;

namespace Netlyt.Service.Integration
{
    public class DateParser
    {
        public bool TryParse(string value, out DateTime timeValue, out double? doubleValue)
        {
            double doubleValueTmp;
            if (Double.TryParse(value, out doubleValueTmp))
            {
                doubleValue = doubleValueTmp;
                timeValue = DateTime.MinValue;
                return false;
            }
            doubleValue = null;
            var pvd = CultureInfo.InvariantCulture;
            if (DateTime.TryParse(value, pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yy", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd/MM/yyyy H:mm:ss", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd/MM/yyyy H:mm", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd/MM/yy H:mm:ss", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd/MM/yy H:mm", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yyyy H:mm:ss", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yy H:mm:ss", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yy H:mm", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yy HH:mm:ss", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else if (DateTime.TryParseExact(value, "dd-MM-yy HH:mm", pvd, DateTimeStyles.AssumeUniversal, out timeValue))
            {
                return true;
            }
            else
            {
                timeValue = DateTime.MinValue;
            } 
            return false;
        }
    }
}