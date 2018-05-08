using Donut.Data;
using Netlyt.Interfaces;

namespace Donut
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