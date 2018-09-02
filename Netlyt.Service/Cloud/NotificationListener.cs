using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Cloud.Auth;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud
{
    public class NotificationListener : NotificationExchange
    {
        private IModel channel;
        public event EventHandler<JsonNotification> OnIntegrationCreated;
        public event EventHandler<JsonNotification> OnIntegrationViewed;
        public event EventHandler<JsonNotification> OnPermissionsUpdated;

        public NotificationListener(IModel channel) : base(channel)
        {
            this.channel = channel;
        }

        public void Start()
        {
            ConsumeQuotaNotifications();
            ConsumeIntegrationNotifications();
            ConsumeUserAuthNotifications();
            ConsumePermissionNotifications();
        }

        #region Consume setup methods
        private void ConsumeQuotaNotifications()
        {
            var requestConsumer = new EventingBasicConsumer(channel);
            requestConsumer.Received += OnQuotaNotification;
            channel.BasicConsume(queue: Queues.MessageNotification,
                autoAck: false,
                consumer: requestConsumer);
        }

        private void ConsumeUserAuthNotifications()
        {
            var login = new EventingBasicConsumer(channel);
            var register = new EventingBasicConsumer(channel);
            login.Received += Login_Received;
            register.Received += Register_Received;
            channel.BasicConsume(Queues.UserLogin, false, login);
            channel.BasicConsume(Queues.UserRegister, false, register);
        }

        private void ConsumePermissionNotifications()
        {
            var permCreate = new EventingBasicConsumer(channel);
            permCreate.Received += Permissions_Updated;
            channel.BasicConsume(Queues.PermissionsUpdate, false, permCreate);
        }


        public void ConsumeIntegrationNotifications()
        {
            var newIntegrationConsumer = new EventingBasicConsumer(channel);
            var integrationViewedConsumer = new EventingBasicConsumer(channel);
            newIntegrationConsumer.Received += NewIntegrationConsumer_Received;
            integrationViewedConsumer.Received += IntegrationViewedConsumer_Received;
            channel.BasicConsume(Queues.IntegrationCreated, false, newIntegrationConsumer);
            channel.BasicConsume(Queues.IntegrationViewed, false, integrationViewedConsumer);
        }
        #endregion

        #region Consumer methods
        private void Register_Received(object sender, BasicDeliverEventArgs e)
        {
            channel.BasicAck(e.DeliveryTag, false);
        }

        private void Permissions_Updated(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var notification = JsonNotification.FromRequest(e);
                OnPermissionsUpdated?.Invoke(this, notification);
            }
            catch (Exception ex)
            {
                this.Ack(e);
            }
        }

        private void Login_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var notification = JsonNotification.FromRequest(e);
            }
            catch (Exception ex)
            {
                this.Ack(e);
            }
        }


        private void IntegrationViewedConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var notification = JsonNotification.FromRequest(e);
                OnIntegrationViewed?.Invoke(this, notification);
            }
            catch (Exception)
            {
                this.Ack(e);
            }
        }

        private void NewIntegrationConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var notification = JsonNotification.FromRequest(e);
                OnIntegrationCreated?.Invoke(this, notification);
            }
            catch (Exception)
            {
                this.Ack(e);
            }
        }

        private void OnQuotaNotification(object sender, BasicDeliverEventArgs e)
        {
            channel.BasicAck(e.DeliveryTag, false);
        }
        #endregion
    }
}
