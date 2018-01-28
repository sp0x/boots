using System;
using System.Collections.Generic;

namespace Netlyt.Service.Time
{
    public class DateHelper
    {
        private static Dictionary<int, HashSet<DateTime>> HolidayDict = new Dictionary<int, HashSet<DateTime>>();
        //private static DateHelper _instance;
        private static readonly object _lock = new object();
        static DateHelper()
        {
            int year = DateTime.Now.Year;
            //Load bg holidays
            var holidays = GetHolidays(year);
            var holidaySet = new HashSet<DateTime>();
            foreach (var hol in holidays)
            {
                holidaySet.Add(hol);
            }
            HolidayDict.Add(year, holidaySet);
        }

        private static void LoadYearHolidays(int year)
        {
            lock (_lock)
            {
                if (HolidayDict.ContainsKey(year)) return;
                var holidays = GetHolidays(year);
                var holidaySet = new HashSet<DateTime>();
                foreach (var hol in holidays)
                {
                    holidaySet.Add(hol);
                }
                HolidayDict.Add(year, holidaySet);
            }
        }

        public static bool IsHoliday(DateTime day)
        {
            LoadYearHolidays(day.Year);
            return HolidayDict[day.Year].Contains(day);
        }

        private static HashSet<DateTime> GetHolidays(int year)
        {
            HashSet<DateTime> holidays = new HashSet<DateTime>();
            //NEW YEARS 
            DateTime newYearsDate = AdjustForWeekendHoliday(new DateTime(year, 1, 1).Date);
            holidays.Add(newYearsDate);

            //Bg independence day
            DateTime freedom = AdjustForWeekendHoliday(new DateTime(year, 3, 3).Date);
            holidays.Add(freedom);

            //Easter day
            DateTime easter = AdjustForWeekendHoliday(new DateTime(year, 3, 3).Date);
            switch (year)
            {
                case 2017: easter = AdjustForWeekendHoliday(new DateTime(year, 4, 16).Date); break;
                case 2018: easter = AdjustForWeekendHoliday(new DateTime(year, 4, 8).Date); break;
                case 2019: easter = AdjustForWeekendHoliday(new DateTime(year, 4, 28).Date); break;
                case 2020: easter = AdjustForWeekendHoliday(new DateTime(year, 4, 19).Date); break;
                default:
                    throw new NotImplementedException();
            }
            holidays.Add(easter);

            //Labour day
            DateTime labourDay = AdjustForWeekendHoliday(new DateTime(year, 5, 1).Date);
            DayOfWeek dayOfWeek = labourDay.DayOfWeek;
            while (dayOfWeek != DayOfWeek.Monday)
            {
                labourDay = labourDay.AddDays(1);
                dayOfWeek = labourDay.DayOfWeek;
            }
            holidays.Add(labourDay.Date);

            //stGeorge day
            DateTime stGeorge = AdjustForWeekendHoliday(new DateTime(year, 5, 6).Date);
            holidays.Add(stGeorge.Date);

            //Slavic culture day
            DateTime slavicCulture = AdjustForWeekendHoliday(new DateTime(year, 5, 24).Date);
            holidays.Add(slavicCulture.Date);


            //Bg join day
            DateTime nationalJoiningDay = AdjustForWeekendHoliday(new DateTime(year, 9, 6).Date);
            holidays.Add(nationalJoiningDay.Date);


            //Bg independence day
            DateTime independanceDay = AdjustForWeekendHoliday(new DateTime(year, 9, 22).Date);
            holidays.Add(independanceDay.Date);

            //Buditeli day
            DateTime buditeliDay = AdjustForWeekendHoliday(new DateTime(year, 11, 1).Date);
            holidays.Add(buditeliDay.Date);

            //Budni vecher day
            DateTime bChristmas = AdjustForWeekendHoliday(new DateTime(year, 12, 24).Date);
            holidays.Add(bChristmas.Date);

            //Budni vecher day
            DateTime xmas1 = AdjustForWeekendHoliday(new DateTime(year, 12, 25).Date);
            holidays.Add(xmas1.Date);
            //Budni vecher day
            DateTime xmas2 = AdjustForWeekendHoliday(new DateTime(year, 12, 26).Date);
            holidays.Add(xmas2.Date);

            return holidays;
        }

        public static DateTime AdjustForWeekendHoliday(DateTime holiday)
        {
            if (holiday.DayOfWeek == DayOfWeek.Saturday)
            {
                return holiday.AddDays(-1);
            }
            else if (holiday.DayOfWeek == DayOfWeek.Sunday)
            {
                return holiday.AddDays(1);
            }
            else
            {
                return holiday;
            }
        }
    }
}