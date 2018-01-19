using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public class DonutContext 
        : EntityMetaContext
    {  
        public DonutContext(RedisCacher cacher) : base(cacher)
        {
            var members = this.GetType().GetProperties();
            members = members;
        } 

        public void Cache()
        {

            throw new System.NotImplementedException();
        }
    }
}