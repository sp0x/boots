using System.Threading.Tasks; 
using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    public interface IOrionContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orionQuery"></param>
        /// <returns></returns>
        Task<JToken> Query(OrionQuery orionQuery);
        event OrionEventsListener.OrionEventHandler NewMessage;
        event FeaturesGenerated FeaturesGenerated;
        event TrainingComplete TrainingComplete;
        event TrainingComplete PredictionReady;
        string GetExperimentAsset(string path);
    }
}