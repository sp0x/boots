using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using nvoid.db.DB.Configuration;
using Newtonsoft.Json.Linq;
using Netlyt.Service.Integration; 
using Netlyt.Service.Orion;

namespace Netlyt.Service
{
    public class OrionContext
    {
        private OrionClient _client;
        private string _destinationIp;
        private int _inputPort;
        private int _outputPort;
        private ITargetBlock<IntegratedDocument> _actionBlock;

        public OrionContext()
        {
            _client = new OrionClient();
            _actionBlock = new ActionBlock<IntegratedDocument>((doc) =>
            {
                _client.SendDocument(doc);
            });
        }
         
        
        /// <summary>
        /// Configure the context
        /// </summary>
        /// <param name="configSection"></param>
        public void Configure(IConfigurationSection configSection)
        { 
            var mqSection = configSection.GetSection("mq");
            if (mqSection==null)
            {
                throw new System.Exception("Invalid or no MQ configuration supplied!");
            }

            MqConfiguration mqConfig = new MqConfiguration();
            mqSection.Bind(mqConfig);
            if (mqConfig == null)
            {
                throw new System.Exception("Invalid or no MQ configuration supplied!");
            }
            Debug.WriteLine($"Mq input: {mqConfig.InputPort}");
            Console.WriteLine($"Mq input: {mqConfig.InputPort}");

            Debug.WriteLine($"Mq output: {mqConfig.OutputPort}");
            Console.WriteLine($"Mq output: {mqConfig.OutputPort}");

            _inputPort = mqConfig.InputPort; 
            _outputPort = mqConfig.OutputPort;
            _destinationIp = mqConfig.Destination;
        }

        public void Run()
        {
            _client.ConnectAsync(_destinationIp, _inputPort, _outputPort);
        }

        /// <summary>   Sends a raw string message. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="message">  The message. </param>

        public void SendMessage(string message)
        {
            _client.SendMessage(message);
        }

        public async Task<JToken> Query(JToken query)
        {
            return await _client.Query(query);
        }
        /// <summary>
        /// Gets the behaviour submission block
        /// </summary>
        /// <returns></returns>
        public ITargetBlock<IntegratedDocument> GetActionBlock()
        {
            return _actionBlock;
        }
    }
}
