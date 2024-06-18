using System;
using System.Buffers;
using RabbitMQ.Client;

namespace ServiceStack.RabbitMq;

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

    public override void OnCancel(params string[] consumerTags)
    {
        queue.Close();
        base.OnCancel();
    }
        
    public override void HandleBasicDeliver(
        string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
        string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> readOnlyMemory)
    {
        // ReadOnlyMemory<byte> not accessible after handler completes https://www.rabbitmq.com/client-libraries/dotnet-api-guide#consuming-memory-safety
        var copy = readOnlyMemory.ToArray();
        
        var msgResult = new BasicGetResult(
            deliveryTag: deliveryTag,
            redelivered: redelivered,
            exchange: exchange,
            routingKey: routingKey,
            messageCount: 0, //Not available, received by RabbitMQ when declaring queue
            basicProperties: properties, copy);

        queue.Enqueue(msgResult);
    }
}