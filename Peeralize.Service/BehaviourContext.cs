using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Peeralize.Service.Network;

namespace Peeralize.Service
{
    public class BehaviourContext
    {
        private BehaviourClient _client;
        private string _destinationIp;
        private int _inputPort;
        private int _outputPort;

        public BehaviourContext()
        {
            _client = new BehaviourClient();
        }

        /// <summary>
        /// Configure the context
        /// </summary>
        /// <param name="configSection"></param>
        public void Configure(IConfigurationSection configSection)
        { 
            var mqConfiguration = configSection.GetSection("mq");
            if (mqConfiguration==null)
            {
                throw new System.Exception("Invalid or no MQ configuration supplied!");
            }
            _inputPort = int.Parse(mqConfiguration["InputPort"]);
            _outputPort = int.Parse(mqConfiguration["OutputPort"]);
            _destinationIp = (mqConfiguration["Destination"]);
        }

        public void Run()
        {
            _client.ConnectAsync(_destinationIp, _inputPort, _outputPort);
        }

        public void SendMessage(string message)
        {
            _client.SendMessage(message);
        }

        public async Task<JToken> Query(JToken query)
        {
            return await _client.Query(query);
        }
    }
}
