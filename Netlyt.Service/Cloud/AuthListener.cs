using System;
using Netlyt.Service.Cloud.Auth;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class AuthListener : AuthExchange
    {
        private IModel channel;
        private AuthMode authMode;
        public event EventHandler<AuthenticationRequest> AuthenticationRequested;

        public AuthListener(IModel channel, AuthMode authMode) : base(channel)
        {
            this.channel = channel;
            this.authMode = authMode;
            if (authMode == AuthMode.Master)
            {
                ConsumeAuthRequests();
            }
        }

        private void ConsumeAuthRequests()
        {
            var requestConsumer = new EventingBasicConsumer(channel);
            requestConsumer.Received += OnAuthRequest;
            channel.BasicConsume(queue: Queues.AuthorizeNode,
                autoAck: false,
                consumer: requestConsumer);
        }

        /// <summary>
        /// Whenever a slave node requests authentication.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAuthRequest(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var authRequest = AuthenticationRequest.FromRequest(e);
                Console.WriteLine("Auth request from: " + authRequest.From);
                AuthenticationRequested?.Invoke(this, authRequest);
            }
            catch (Exception ex)
            {
                var props = channel.CreateBasicProperties();
                props.CorrelationId = e.BasicProperties.CorrelationId;
                channel.BasicPublish(exchange: Exchanges.Auth,
                                    routingKey: e.BasicProperties.ReplyTo,
                                    basicProperties: props,
                                    body: Errors.AuthorizationFailed(ex));
                channel.BasicAck(e.DeliveryTag, false);
            }
        }

        

    }
}
