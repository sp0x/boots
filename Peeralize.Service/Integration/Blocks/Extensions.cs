using System.Linq;
using System.Collections.Generic;
using System;

namespace Extensions
{
    public static class ListExtension
    {
        public static T Pop<T>(this List<T> list)
        {
            T r = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return r;
        }
    }

}

