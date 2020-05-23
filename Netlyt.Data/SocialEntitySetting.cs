using System;

namespace Netlyt.Data
{
    public class SocialEntitySetting// : ConfigurationElement, IConfigElement
    {
        //[ConfigurationProperty("weeks", IsRequired = false)]
        public int Weeks { get; set; } //=> (int)base["weeks"];

        //[ConfigurationProperty("days", IsRequired = false)]
        public int Days { get; set; }// => (int)base["days"];


        //[ConfigurationProperty("hours", IsRequired = false)]
        public int Hours { get; set; } //=> (int)base["hours"];

        //[ConfigurationProperty("minutes", IsRequired = false)]
        public int Minutes { get; set; }// => (int)base["minutes"];

        //[ConfigurationProperty("seconds", IsRequired = false)]
        public int Seconds { get; set; }// => (int)base["seconds"];

        //[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name { get; set; }// => (string)base["name"];

        public bool IsGeneral => Name.ToLower() == "general";

        public SocialEntitySetting() { }
         
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets the period of this setting.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTotalPeriod()
        {
            var ts = new TimeSpan();
            if (Weeks > 0)  ts = ts + TimeSpan.FromDays(Weeks * 7);
            if (Days > 0) ts = ts + TimeSpan.FromDays(Days);
            if (Hours > 0)  ts = ts + TimeSpan.FromHours(Hours);
            if (Minutes > 0)  ts = ts + TimeSpan.FromMinutes(Minutes);
            if (Seconds > 0)  ts = ts + TimeSpan.FromSeconds(Seconds);
            
            return ts;
        }

        /// <summary>
        /// Checks if the initial date has passed the maximum allowed period defined by this setting.
        /// </summary>
        /// <param name="initialDate"></param>
        /// <returns></returns>
        public bool HasPassed(DateTime initialDate)
        {
            var now = DateTime.Now;
            var diff = now - initialDate;
            var period = GetTotalPeriod();
            return diff > period;
        }

        public bool HasNotPassed(DateTime initialDate)
        {
            return !HasPassed(initialDate);
        }


    }
}