using nvoid.db;
using Netlyt.Interfaces;

namespace Netlyt.Service.Donut
{
    public class InternalDataSet<T> : DataSet<T> where T : class
    { 

        public InternalDataSet(ISetCollection context)
        { 
        }

        protected InternalDataSet()
        {
        }
    }
}