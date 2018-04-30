using System;
using System.Collections.Generic;
using System.Text;

namespace Donut.Blocks
{
    public static class Extensions
    {
        /// <summary>
        /// Converts the string to a hostname
        /// </summary>
        /// <param name="s"></param>
        /// <param name="removeWww"></param>
        /// <returns></returns>
        public static string ToHostname(this string s, bool removeWww = false)
        {
            if (s == null)
                return null;
            if (s.StartsWith("http://"))
                s = s.Substring(7);
            if (s.StartsWith("https://"))
                s = s.Substring(8);
            if (s.Contains("/"))
            {
                s = s.Substring(0, s.IndexOf("/"));
            }
            if (removeWww)
            {
                if (s.StartsWith("www."))
                    s = s.Substring(4);
            }
            //If s.StartsWith("www.") Then s = s.Substring(4)
            //If s.StartsWith("m.") Then s = s.Substring(2)
            if (s.Contains("@"))
            {
                s = s.Substring(s.IndexOf("@") + 1);
            }
            return s;
        }
    }
}
