
using RabbitMQ.Client;
using RabbitMQ.Util;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqBasicConsumer : DefaultBasicConsumer
    {
        readonly SharedQueue<BasicGetResult> queue;

        public RabbitMqBasicConsumer(IModel model) 
            : this(model, new SharedQueue<BasicGetResult>()) { }

        public RabbitMqBasicConsumer(IModel model, SharedQueue<BasicGetResult> queue)
            : base(model)
        {
            this.queue = queue;
        }

        public SharedQueue<BasicGetResult> Queue => queue;

        public override void OnCancel()
        {
            queue.Close();
            base.OnCancel();
        }

        public override void HandleBasicDeliver(
            string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
            string routingKey, IBasicProperties properties, byte[] body)
        {
            var msgResult = new BasicGetResult(
                deliveryTag: deliveryTag,
                redelivered: redelivered,
                exchange: exchange,
                routingKey: routingKey,
                messageCount: 0, //Not available, received by RabbitMQ when declaring queue
                basicProperties: properties,
                body: body);

            queue.Enqueue(msgResult);
        }
    }
}