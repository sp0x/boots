using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    public interface IOrionContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orionQuery"></param>
        /// <returns></returns>
        Task<JToken> Query(OrionQuery orionQuery);
    }
}