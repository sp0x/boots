using MongoDB.Driver;
using nvoid.db;
using nvoid.db.Caching;
using nvoid.db.DB;

namespace Netlyt.Service.Donut
{
    public class InternalDataSet<T> : DataSet<T> where T : class
    { 

        public InternalDataSet(ISetCollection context)
        { 
        }
    }
}