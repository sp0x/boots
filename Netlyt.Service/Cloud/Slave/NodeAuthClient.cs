using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Cloud.Interfaces;
using Netlyt.Service.Repisitories;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Slave
{
    public class NodeAuthClient : AuthExchange, IAuthWriter
    {
        public int AuthTimeout { get; set; } = 1000 * 60 * 60 * 24;
        private AuthListener _authListener;
        private IModel _channel;
        public QueueDeclareOk CallbackQueue { get; private set; }
        public event EventHandler<AuthenticationResponse> OnAuthenticated;
        public string AuthenticationToken { get; private set; } 

        private ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>> _requests;
        private EventingBasicConsumer _callbackConsumer;
        private IUsersRepository _users;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public NodeAuthClient(IModel channel) : base(channel)
        {
            _authListener = new AuthListener(channel, AuthMode.Client);
            _channel = channel;
            CallbackQueue = channel.QueueDeclare(exclusive: true);
            channel.QueueBind(CallbackQueue.QueueName, Exchanges.Auth, CallbackQueue.QueueName);
            _requests = new ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>();
            _callbackConsumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(CallbackQueue.QueueName, true, _callbackConsumer);
            _callbackConsumer.Received += OnCallback;
        }

        private void OnCallback(object sender, BasicDeliverEventArgs e)
        {
            TaskCompletionSource<BasicDeliverEventArgs> completionSource;
            if (_requests.TryGetValue(e.BasicProperties.CorrelationId, out completionSource))
            {
                completionSource.TrySetResult(e);
            }
        }

        /// <summary>
        /// Creates a new awaiter that waits for a message
        /// </summary>
        /// <returns></returns>
        private TaskCompletionSource<BasicDeliverEventArgs> CreateMessageAwaiter(string correlationId)
        {
            var awaiter = new TaskCompletionSource<BasicDeliverEventArgs>();
            //Console.WriteLine("Adding request with id: " + awaiterId);
            _requests.TryAdd(correlationId, awaiter);
            return awaiter;
        }

        public async Task<Tuple<bool, User>> LoginUser(string email, string password)
        {
            var body = JToken.FromObject(new
            {
                email = email,
                password = password
            });
            var bodyBytes = Encoding.UTF8.GetBytes(body.ToString());
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ReplyTo = CallbackQueue.QueueName;
            props.CorrelationId = Guid.NewGuid().ToString();
            Console.WriteLine("Sending login request: " + props.CorrelationId);
            props.Expiration = (AuthTimeout).ToString();
            var awaitCompletionTask = CreateMessageAwaiter(props.CorrelationId);
            _channel.BasicPublish(exchange: Exchanges.Auth,
                routingKey: Routes.UserLoginForNode,
                basicProperties: props,
                body: bodyBytes);
            var authTimeoutTask = Task.Delay(AuthTimeout);
            var resultingTask = await Task.WhenAny(authTimeoutTask, awaitCompletionTask.Task);
            if (resultingTask.IsFaulted)
            {
                throw new AuthenticationFailed();
            }
            else if (resultingTask.Id == authTimeoutTask.Id)
            {
                throw new TimeoutException("Authentication timed out.");
            }
            else
            {
                var response = awaitCompletionTask.Task.Result as BasicDeliverEventArgs;
                try
                {
                    var result = response.GetJson();
                    var successfull = result["success"] as JValue;
                    if (!((bool)successfull))
                    {
                        throw new AuthenticationFailed();
                    }
                    else
                    {
                        var userObj = result["user"].ToObject<User>();
                        return new Tuple<bool, User>(true, userObj);
                    }
                }
                catch (Exception ex)
                {
                    throw new AuthenticationFailed();
                }
            }
        }


        public async Task<AuthenticationResponse> AuthorizeCloudNode(NetlytNode node)
        {
            byte[] body = CreateCloudAuthPayload(node);
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ReplyTo = CallbackQueue.QueueName;
            props.CorrelationId = Guid.NewGuid().ToString();
            props.Expiration = (AuthTimeout).ToString();
            var awaitCompletionTask = CreateMessageAwaiter(props.CorrelationId);
            _channel.BasicPublish(exchange: Exchanges.Auth,
                routingKey: Routes.AuthorizeNode,
                basicProperties: props,
                body: body);
            var authTimeoutTask = Task.Delay(AuthTimeout);
            var resultingTask = await Task.WhenAny(authTimeoutTask, awaitCompletionTask.Task);
            if (resultingTask.IsFaulted)
            {
                throw new AuthenticationFailed();
            }
            else if (resultingTask.Id == authTimeoutTask.Id)
            {
                throw new TimeoutException("Authentication timed out.");
            }
            else
            {
                try
                {
                    BasicDeliverEventArgs authResponse = awaitCompletionTask.Task.Result;
                    var response = AuthenticationResponse.FromRequest(authResponse);
                    var result = response.Result;
                    var successfull = result["success"] as JValue;
                    if (!((bool)successfull))
                    {
                        throw new AuthenticationFailed();
                    }
                    else
                    {
                        AuthenticationToken = response.Result["token"].ToString();
                        OnAuthenticated?.Invoke(this, response);
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    throw new AuthenticationFailed();
                }
            }
        }

        public async Task<AuthenticationResponse> AuthorizeNode(NetlytNode node)
        {
            var body = CreateAuthPayload(node);
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ReplyTo = CallbackQueue.QueueName;
            props.CorrelationId = Guid.NewGuid().ToString();
            props.Expiration = (AuthTimeout).ToString();
            var awaitCompletionTask = CreateMessageAwaiter(props.CorrelationId);
            _channel.BasicPublish(exchange: Exchanges.Auth,
                routingKey: Routes.AuthorizeNode,
                basicProperties: props,
                body: body);
            var authTimeoutTask = Task.Delay(AuthTimeout);
            var resultingTask = await Task.WhenAny(authTimeoutTask, awaitCompletionTask.Task);
            if (resultingTask.IsFaulted)
            {
                throw new AuthenticationFailed();
            }
            else if (resultingTask.Id == authTimeoutTask.Id)
            {
                throw new TimeoutException("Authentication timed out.");
            }
            else
            {
                try
                {
                    var authResponse = awaitCompletionTask.Task.Result;
                    var response = AuthenticationResponse.FromRequest(authResponse);
                    var result = response.Result;
                    var successfull = result["success"] as JValue;
                    if (!((bool)successfull))
                    {
                        throw new AuthenticationFailed();
                    }
                    else
                    {
                        AuthenticationToken = response.Result["token"].ToString();
                        OnAuthenticated?.Invoke(this, response);
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    throw new AuthenticationFailed();
                }
            }
        }

        private static byte[] CreateAuthPayload(NetlytNode node)
        {
            var authPayload = new MemoryStream();
            var streamWriter = new StreamWriter(authPayload) {AutoFlush = true};
            streamWriter.Write(node.ApiKey.AppId);
            streamWriter.Write("//\\\\");
            streamWriter.Write(node.ApiKey.AppSecret);
            var rawBytes = authPayload.ToArray();
            var authPayloadStr = Convert.ToBase64String(rawBytes);
            var body = Encoding.ASCII.GetBytes(authPayloadStr);
            return body;
        }

        private static byte[] CreateCloudAuthPayload(NetlytNode node)
        {
            var authPayload = new MemoryStream();
            var streamWriter = new StreamWriter(authPayload) { AutoFlush = true };
            streamWriter.Write("__cloud__;");
            streamWriter.Write(node.Name);
            var rawBytes = authPayload.ToArray();
            var authPayloadStr = Convert.ToBase64String(rawBytes);
            var body = Encoding.ASCII.GetBytes(authPayloadStr);
            return body;
        }
         
    }

}
