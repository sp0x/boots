using System;
using System.Text;
using nvoid.db.Caching;
using Netlyt.Service.Integration;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    public class Donutfile<TContext> : IDisposable
        where TContext : DonutContext
    {
        public TContext Context { get; set; }
        public Donutfile(RedisCacher cacher)
        { 
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
