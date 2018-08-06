using System;
using System.Collections.Generic;
using System.Text;

namespace Donut.Data
{
    public class Cleanup
    {
        public static string CleanupFieldName(string fieldName)
        {
            return fieldName.Replace("-", "_").Replace(".", "_").Replace(" ", "_").Replace("<", "_")
                .Replace(">", "_").Replace("=", "_").Replace("+", "_")
                .Replace("$", "_").Replace("#", "_").Replace("*", "_")
                .Replace("(", "_").Replace(")", "_");
        }
    }
}
