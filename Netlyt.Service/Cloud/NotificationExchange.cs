using System.Collections.Generic;
using Netlyt.Service.Cloud.Interfaces;
using RabbitMQ.Client;

namespace Netlyt.Service.Cloud
{
    public abstract class NotificationExchange : IRPCableExchange
    {
        public IModel Channel { get; private set; }
        public string Name { get; private set; }
        protected NotificationExchange(IModel channel)
        {
            var exchange = Exchanges.Notifications;
            Name = exchange;
            this.Channel = channel;
            channel.ExchangeDeclare(exchange: Exchanges.Notifications,
                type: ExchangeType.Topic, durable: true, autoDelete: false);
            channel.QueueDeclare(queue: Queues.MessageNotification, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.UserLogin, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.UserRegister, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.PermissionsSet, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.IntegrationCreated, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.IntegrationViewed, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.ModelStageUpdate, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.ModelEdit, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.ModelCreate, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.ModelBuild, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(queue: Queues.QuotaUpdate, durable: true, exclusive: false, autoDelete: false);

            var specs = new Dictionary<string, object>();
            specs["x-match"] = "any";
            //specs["type"] = "";
            channel.QueueBind(exchange: exchange,
                queue: Queues.MessageNotification, routingKey: Routes.MessageNotification);
            channel.QueueBind(exchange: exchange,
                queue: Queues.UserLogin, routingKey: Routes.UserLoginNotification);
            channel.QueueBind(exchange: exchange, queue: Queues.UserRegister, routingKey: Routes.UserRegisterNotification);
            channel.QueueBind(exchange: exchange, queue: Queues.PermissionsSet, routingKey: Routes.PermissionsSet);
            channel.QueueBind(exchange: exchange, queue: Queues.IntegrationCreated, routingKey: Routes.IntegrationCreated);
            channel.QueueBind(exchange: exchange, queue: Queues.IntegrationViewed, routingKey: Routes.IntegrationViewed);
            channel.QueueBind(exchange: exchange, queue: Queues.ModelStageUpdate, routingKey: Routes.ModelStageUpdate);
            channel.QueueBind(exchange: exchange, queue: Queues.ModelEdit, routingKey: Routes.ModelEdit);
            channel.QueueBind(exchange: exchange, queue: Queues.ModelCreate, routingKey: Routes.ModelCreate);
            channel.QueueBind(exchange: exchange, queue: Queues.ModelBuild, routingKey: Routes.ModelBuild);
            channel.QueueBind(exchange: exchange, queue: Queues.QuotaUpdate, routingKey: Routes.QuotaUpdate);
        }


    }
}