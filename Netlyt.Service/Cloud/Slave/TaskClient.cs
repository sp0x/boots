using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Slave
{
    public class TaskClient : TaskExchange
    {
        private EventingBasicConsumer _consumer;
        public QueueDeclareOk TaskQueue { get; set; }
        public event EventHandler<Tuple<string, BasicDeliverEventArgs>> OnCommand;

        public TaskClient(IModel channel, string token) : base(channel)
        {
            TaskQueue = channel.QueueDeclare(exclusive: true);
            var specs = new Dictionary<string, object>();
            specs["x-match"] = "all";
            specs["token"] = token;
            channel.ExchangeBind(source: Name, destination: TaskQueue.QueueName, routingKey: "", arguments: specs);
            _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += _consumer_Received;
            channel.BasicConsume(TaskQueue.QueueName, false, _consumer);
        }

        private void _consumer_Received(object sender, BasicDeliverEventArgs e)
        {

            object subCommand = null;
            if (e.BasicProperties.Headers.TryGetValue("cmd", out subCommand))
            {
                OnCommand?.Invoke(this, new Tuple<string, BasicDeliverEventArgs>(subCommand.ToString(), e));
            }
            else
            {
                Channel.BasicAck(e.DeliveryTag, false);
            }
        }
         
    }
}