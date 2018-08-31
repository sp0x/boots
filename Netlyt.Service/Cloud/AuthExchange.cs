using System.Collections.Generic;
using Netlyt.Service.Cloud.Interfaces;
using RabbitMQ.Client;

namespace Netlyt.Service.Cloud
{
    public abstract class TaskExchange : IRPCableExchange
    {
        public IModel Channel { get; private set; }
        public string Name { get; private set; }

        public TaskExchange(IModel channel)
        {
            this.Channel = channel;
            this.Name = Exchanges.Tasks;
            channel.ExchangeDeclare(exchange: Name, type: ExchangeType.Headers, durable: true, autoDelete: false);
        }
    }

    public abstract class AuthExchange : IRPCableExchange
    {
        public IModel Channel { get; private set; }
        public string Name { get; }

        public AuthExchange(IModel channel)
        {
            this.Channel = channel;
            this.Name = Exchanges.Auth;
            channel.ExchangeDeclare(exchange: Name,
                type: ExchangeType.Direct, durable: true, autoDelete: false);
            //Declare our authorizations queue
            channel.QueueDeclare(queue: Queues.AuthorizeNode,
                durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.UserLoginForNode,
                durable: true, exclusive: false, autoDelete: false);
            var specs = new Dictionary<string, object>();
            channel.QueueBind(queue: Queues.AuthorizeNode,
                exchange: Exchanges.Auth,
                routingKey: Routes.AuthorizeNode,
                arguments: specs);
            channel.QueueBind(queue: Queues.UserLoginForNode,
                exchange: Exchanges.Auth,
                routingKey: Routes.UserLoginForNode);
        }
    }
}