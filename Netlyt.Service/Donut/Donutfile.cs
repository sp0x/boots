using System.Text;
using nvoid.db.Caching;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    public class Donutfile
    {
        public Donutfile(RedisCacher cacher)
        { 
        }
    }
}
/**
* TODO:
* - pass in all reduced documents to be analyzed
* - join any additional integration sources/ raw or reduced collections
* - analyze and extract metadata (variables) about the dataset
* - generate features for every constructed document (initial reduced document + any additional data) using analyzed metadata.
* -- Use redis to cache the gathered metadata from generating the required variables
* */

/**
 * Code generation style:
 * each feature generation should be a method, for readability and easy debugging/tracking
 **/
