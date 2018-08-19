namespace Netlyt.Interfaces.Models
{
    public class ApiRateLimit
    {
        public long Id { get; set; }
        public int Daily { get; set; }
        public int Monthly { get; set; }
        public int Weekly { get; set; }
        public string Name { get; set; }

        public ApiRateLimit()
        {

        }
        public static ApiRateLimit operator -(ApiRateLimit a, ApiRateLimit b)
        {
            var output = new ApiRateLimit();
            output.Daily = a.Daily - b.Daily;
            output.Weekly = a.Weekly - b.Weekly;
            output.Monthly = a.Monthly - b.Monthly;
            output.Name = b.Name;
            return output;
        }
        public static ApiRateLimit CreateDefault()
        {
            return new ApiRateLimit
            {
                Name = "Default",
                Daily = 10000,
                Monthly = 10000,
                Weekly = 10000
            };
        }
    }
}