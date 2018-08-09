using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    public delegate void FeaturesGenerated(JObject featureResult);
    public delegate void TrainingComplete(JObject featureResult);
    public delegate void PredictionReady(JObject featureResult);

    public class OrionContext : IOrionContext
    {
        public string Id { get; private set; }
        private OrionClient _client;
        private OrionEventsListener _eventListener;
        private string _destinationIp;
        private int _inputPort;
        private int _outputPort;
        private ITargetBlock<IntegratedDocument> _actionBlock;
        private int _eventsPort;
        private IConfiguration _configuration;

        #region Events
        public event OrionEventsListener.OrionEventHandler NewMessage;
        public event FeaturesGenerated FeaturesGenerated;
        public event TrainingComplete TrainingComplete;
        public event TrainingComplete PredictionReady;

        public string GetExperimentAsset(string path)
        {
            var isAbsolute = false;
            var expPath = OrionSink.GetExperimentsPath(_configuration, out isAbsolute, ref path);
            Trace.WriteLine(expPath);
            Console.WriteLine(expPath);
            var assetPath = System.IO.Path.Combine(expPath, path);
            if (!System.IO.File.Exists(assetPath))
            {
                return null;
            }
            else
            {
                return assetPath;
            }
        }

        #endregion

        public OrionContext(
            IConfiguration configuration)
        {
            Id = Guid.NewGuid().ToString();
            _client = new OrionClient();
            _eventListener = new OrionEventsListener();
            _configuration = configuration;
            _eventListener.NewMessage += HandleNewEventMessage;
            _actionBlock = new ActionBlock<IntegratedDocument>((doc) =>
            {
                _client.SendDocument(doc);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void HandleNewEventMessage(JObject message)
        {
            NewMessage?.Invoke(message);
            var eventParams = message["params"];
            if (eventParams == null) return;
            var type = (OrionOp)int.Parse(eventParams["command"].ToString());
            switch (type)
            {
                case OrionOp.GenerateFeatures:
                    FeaturesGenerated?.Invoke(message);
                    break;
                case OrionOp.Train:
                    TrainingComplete?.Invoke(message);
                    break;
                case OrionOp.MakePrediction:
                    PredictionReady?.Invoke(message);
                    break;
                default:
                    break;
            }
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
            _eventsPort = mqConfig.EventsPort;
            _destinationIp = mqConfig.Destination;
            
        }

        public void Run()
        {
            _client.ConnectAsync(_destinationIp, _inputPort, _outputPort);
            _eventListener.ConnectAsync(_destinationIp, _eventsPort);
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orionQuery"></param>
        /// <returns></returns>
        public async Task<JToken> Query(OrionQuery orionQuery)
        {
            var token = orionQuery.Serialize();
            return await Query(token);
        }
    }
}
