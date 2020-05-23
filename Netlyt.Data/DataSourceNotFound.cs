using System;

namespace Netlyt.Data
{
    internal class DataSourceNotFound : Exception
    {
        public Type DataType { get; set; }
        public DataSourceNotFound(Type type)
        {
            DataType = type;
        }
        public override string Message
        {
            get { return String.Format("DataSource for type {0} has not been found! Please revisit your project configuration.", DataType.ToString()); }
        }
    }
}