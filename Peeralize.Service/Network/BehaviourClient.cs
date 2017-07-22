using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Peeralize.Service.Integration;

namespace Peeralize.Service.Network
{
    /// <summary>
    /// A client for user behaviour analytics
    /// </summary>
    public class BehaviourClient
    {
        private DataSink _writer;
        private DataProducer _reader;
        private Dictionary<int, TaskCompletionSource<JToken>> _requests;
        private int _seq;
        
        public enum BehaviourServerCommand
        {
            DataAvailable = 101,
            MakePrediction = 102
        }


        public BehaviourClient()
        {
            _requests = new Dictionary<int, TaskCompletionSource<JToken>>();
            _writer = new DataSink();
            _reader = new DataProducer();
            _reader.OnMessage += ReaderOnMessage;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationIp"></param>
        /// <param name="inputPort"></param>
        /// <param name="outputPort"></param>
        public void Connect(string destinationIp, int inputPort, int outputPort)
        {
            _writer.Connect(destinationIp, outputPort);
            _reader.Connect(destinationIp, inputPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationIp"></param>
        /// <param name="inputPort"></param>
        /// <param name="outputPort"></param>
        public async void ConnectAsync(string destinationIp, int inputPort, int outputPort)
        {
            await Task.Run(() =>
            {
                Connect(destinationIp, inputPort, outputPort);
            });
        }

        public void SendMessage(string message)
        {
            _writer.Send(message);
        }

        public void SendDocument(IntegratedDocument doc)
        {
            //Normalize the data, then send it
            var data = doc.Document.ToJson().ToString();
            data =  data.Replace("NumberLong(1)", "1");
            SendMessage(data);
        }

        public void SendMessage(JToken message)
        {
            _writer.Send(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="company"></param>
        /// <param name="command"></param>
        /// <param name="data"></param>
        public void SendCompanyData(JObject company, BehaviourServerCommand command, JObject data)
        {
            JObject payload = new JObject();
            payload.Add("company", company);
            payload.Add("op", (int) command);
            payload.Add("data", data);
            SendMessage(payload);
        }

        public async Task<JToken> GetPrediction(JObject company)
        {
            JObject payload = new JObject();
            payload.Add("company", company);
            payload.Add("op", (int)BehaviourServerCommand.MakePrediction);
            var tExpectedReply = await Query(payload, (x) =>
            {
                return true;
            });
            return tExpectedReply;
        }

        /// <summary>
        /// Sends a message to the destination, and awaits a message in response of the same message.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="predicate">Command Filter predicate, if needed..</param>
        /// <returns></returns>
        public async Task<JToken> Query(JToken json, Func<string, bool> predicate = null)
        {
            var awaiter = CreateMessageAwaiter();
            SendMessage(json);
            return await awaiter.Task;
        }

        /// <summary>
        /// Creates a new awaiter that waits for a message
        /// </summary>
        /// <returns></returns>
        private TaskCompletionSource<JToken> CreateMessageAwaiter()
        {
            TaskCompletionSource<JToken> awaiter = new TaskCompletionSource<JToken>();
            _requests.Add(_seq++, awaiter);
            return awaiter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageContent"></param>
        private void ReaderOnMessage(object sender, string messageContent)
        {  
            JObject message = JObject.Parse(messageContent);
            if (message["seq"] != null)
            {
                int seq = int.Parse(message["seq"].ToString());
                TaskCompletionSource<JToken> completionSource = null;
                if (_requests.TryGetValue(seq, out completionSource))
                { 
                    completionSource.TrySetResult(message);
                }
                else
                {
                    throw new Exception("Invalid request sequence number!");
                }
            }
            else
            {
                throw new Exception("Invalid message format!");
            }
        }
    }
}
