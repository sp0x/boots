using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Auth;
using Netlyt.Service.Cloud.Interfaces;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;

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

        public NodeAuthClient(IModel channel) : base(channel)
        {
            _authListener = new AuthListener(channel, AuthMode.Client);
            _channel = channel;
            CallbackQueue = channel.QueueDeclare(exclusive: true);
            channel.QueueBind(CallbackQueue.QueueName, Exchanges.Auth, CallbackQueue.QueueName);
        }

        public async Task<AuthenticationResponse> AuthorizeNode(NetlytNode node)
        {
            var authPayload = new MemoryStream();
            var streamWriter = new StreamWriter(authPayload) { AutoFlush = true};
            streamWriter.Write(node.ApiKey.AppId);
            streamWriter.Write("//\\\\");
            streamWriter.Write(node.ApiKey.AppSecret);
            var rawBytes = authPayload.ToArray();
            var authPayloadStr = Convert.ToBase64String(rawBytes);
            var body = Encoding.ASCII.GetBytes(authPayloadStr);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ReplyTo = CallbackQueue.QueueName;
            props.CorrelationId = Guid.NewGuid().ToString();
            props.Expiration = (AuthTimeout).ToString();
            
            _channel.BasicPublish(exchange: Exchanges.Auth,
                routingKey: Routes.AuthorizeNode,
                basicProperties: props,
                body: body);

            var consumer = new QueueingBasicConsumer(_channel);
            _channel.BasicConsume(CallbackQueue.QueueName, true, consumer);
            //Todo: add timeout
            var authTimeoutTask = Task.Delay(AuthTimeout);
            var authTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var authResponse = consumer.Queue.Dequeue();
                        if (authResponse.BasicProperties.CorrelationId != props.CorrelationId) continue;
                        var response = AuthenticationResponse.FromRequest(authResponse);
                        var result = response.Result;
                        var successfull = result["success"] as JValue;
                        if (!((bool) successfull))
                        {
                            throw new AuthenticationFailed();
                        }
                        else
                        {
                            AuthenticationToken = response.Result["token"].ToString();
                            OnAuthenticated?.Invoke(this, response);
                            return response;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new AuthenticationFailed();
                    }
                }
            });
            var resultingTask = await Task.WhenAny(authTimeoutTask, authTask);
            if (resultingTask.IsFaulted)
            {
                throw new AuthenticationFailed();
            }else if (resultingTask.Id == authTimeoutTask.Id)
            {
                throw new TimeoutException("Authentication timed out.");
            }
            else
            {
                return authTask.Result;
            }
        }
    }

}
