using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using Netlyt.Interfaces;
using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    /// <summary>
    /// A client for user behaviour analytics
    /// </summary>
    public class OrionClient
    {
        private OrionSink _writer;
        private OrionSource _reader;

        public string Id { get; }

        private ConcurrentDictionary<int, TaskCompletionSource<JToken>> _requests;
        private int _seq;
        
        public enum BehaviourServerCommand
        {
            DataAvailable = 101,
            MakePrediction = 102
        }


        public OrionClient()
        {
            Id = Guid.NewGuid().ToString();
            _requests = new ConcurrentDictionary<int, TaskCompletionSource<JToken>>();
            _writer = new OrionSink();
            _reader = new OrionSource("Cl");
            _reader.OnMessage += ReaderOnMessage;
            _reader.Run();
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
                while (true)
                {
                    try
                    {
                        Connect(destinationIp, inputPort, outputPort);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Could not connect to orion node at {destinationIp}:{inputPort}. Trying again in 5s. Err: " +
                            ex.Message);
                        Thread.Sleep(5000);
                    }
                }
            });
        }

        /// <summary>   Sends a message as a raw string. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="message">  The message. </param>

        public void SendMessage(string message)
        {
            _writer.Send(message);
        }

        /// <summary>   Sends a bson document as a JSON string. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="doc">  The document. </param>

        public void SendDocument(IntegratedDocument doc)
        {
            //Normalize the data, then send it
            var data = doc.Document.ToJson().ToString();
            SendMessage(data); 
        }

        /// <summary>   Sends a JToken as a JSON string. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="message">  The message. </param>

        public void SendMessage(JToken message)
        {
            _writer.Send(message);
        }

        public override string ToString()
        {
            return _writer.ToString();
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
            int sqid;
            var awaiter = CreateMessageAwaiter(out sqid);
            json["seq"] = sqid;
            SendMessage(json);
            //var frame = _reader.Receive();
            return await awaiter.Task;
        }

        /// <summary>
        /// Creates a new awaiter that waits for a message
        /// </summary>
        /// <returns></returns>
        private TaskCompletionSource<JToken> CreateMessageAwaiter(out int awaiterId)
        {
            TaskCompletionSource<JToken> awaiter = new TaskCompletionSource<JToken>();
            awaiterId = _seq++;
            //Console.WriteLine("Adding request with id: " + awaiterId);
            _requests.TryAdd(awaiterId, awaiter);
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
#if DEBUG 
                    throw new Exception("Invalid request sequence number!");
#endif
                }
            }
            else
            {
#if DEBUG
                Debug.WriteLine("Invalid message format!");
#endif
            }
        }
    }
}
