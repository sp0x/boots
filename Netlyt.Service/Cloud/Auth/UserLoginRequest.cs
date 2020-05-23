using RabbitMQ.Client.Events;

namespace Netlyt.Service.Cloud.Auth
{
    public class UserLoginRequest : RpcMessage
    {
        public string Email { get; private set; }
        public string Password { get; private set; }
        public UserLoginRequest(string replyTo, string correlationId, ulong deliveryTag) : base(replyTo, correlationId, deliveryTag)
        {
        }

        public static UserLoginRequest FromRequest(BasicDeliverEventArgs e)
        {
            var rq = new UserLoginRequest(e.BasicProperties.ReplyTo, e.BasicProperties.CorrelationId,
                e.DeliveryTag);
            var body = e.GetJson();
            rq.Email = body["email"].ToString();
            rq.Password = body["password"].ToString();
            return rq;
        }
    }
}