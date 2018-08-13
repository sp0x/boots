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