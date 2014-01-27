using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.RabbitMq
{
    public static class RabbitMqExtensions
    {
        public static IModel OpenChannel(this IConnection connection)
        {
            var channel = connection.CreateModel();
            channel.RegisterDirectExchange();
            channel.RegisterDlqExchange();
            channel.RegisterTopicExchange();
            return channel;
        }

        public static void RegisterDirectExchange(this IModel channel, string exchangeName = null)
        {
            channel.ExchangeDeclare(exchangeName ?? QueueNames.Exchange, "direct", durable: true, autoDelete: false, arguments: null);
        }

        public static void RegisterDlqExchange(this IModel channel, string exchangeName = null)
        {
            channel.ExchangeDeclare(exchangeName ?? QueueNames.ExchangeDlq, "direct", durable: true, autoDelete: false, arguments:null);
        }

        public static void RegisterTopicExchange(this IModel channel, string exchangeName = null)
        {
            channel.ExchangeDeclare(exchangeName ?? QueueNames.ExchangeTopic, "fanout", durable: false, autoDelete: false, arguments:null);
        }

        public static void RegisterQueue(this IModel channel, string queueName)
        {
            var args = new Dictionary<string, object> {
                {"x-dead-letter-exchange", QueueNames.ExchangeDlq },
                {"x-dead-letter-routing-key", queueName.Replace(".inq",".dlq").Replace(".priorityq",".dlq") },
            };
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueBind(queueName, QueueNames.Exchange, routingKey: queueName);
        }

        public static void RegisterDlq(this IModel channel, string queueName)
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queueName, QueueNames.ExchangeDlq, routingKey: queueName);
        }

        public static void RegisterTopic(this IModel channel, string queueName)
        {
            channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queueName, QueueNames.ExchangeTopic, routingKey: queueName);
        }

        public static void DeleteQueue<T>(this IModel model)
        {
            var queueNames = new[] {
                QueueNames<T>.In,
                QueueNames<T>.Priority,
                QueueNames<T>.Out, //topic is non-durable
                QueueNames<T>.Dlq,
            };
            model.DeleteQueues(queueNames);
        }

        public static void DeleteQueues(this IModel channel, params string[] queues)
        {
            foreach (var queue in queues)
            {
                try
                {
                    channel.QueueDelete(queue, ifUnused:true, ifEmpty:true);
                }
                catch (OperationInterruptedException ex)
                {
                    if (!ex.Message.Contains("code=404"))
                        throw;
                }
            }
        }

        public static void PurgeQueue<T>(this IModel model)
        {
            var queueNames = new[] {
                QueueNames<T>.In,
                QueueNames<T>.Priority,
                QueueNames<T>.Out, //topic is non-durable
                QueueNames<T>.Dlq,
            };
            model.PurgeQueues(queueNames);
        }

        public static void PurgeQueues(this IModel model, params string[] queues)
        {
            foreach (var queue in queues)
            {
                try
                {
                    model.QueuePurge(queue);
                }
                catch (OperationInterruptedException ex)
                {
                    if (!ex.Message.Contains("code=404"))
                        throw;
                }
            }
        }

        public static void PopulateFromMessage(this IBasicProperties props, IMessage message)
        {
            props.MessageId = message.Id.ToString();
            props.Timestamp = new AmqpTimestamp(message.CreatedDate.ToUnixTime());
            props.Priority = (byte)message.Priority;
            props.ContentType = MimeTypes.Json;

            if (message.ReplyTo != null)
            {
                props.ReplyTo = message.ReplyTo;
            }

            if (message.ReplyId != null)
            {
                props.CorrelationId = message.ReplyId.Value.ToString();
            }

            if (message.Error != null)
            {
                props.Headers["Error"] = message.Error.ToJson();
            }
        }

        public static IMessage<T> ToMessage<T>(this BasicGetResult msgResult)
        {
            if (msgResult == null)
                return null;

            var props = msgResult.BasicProperties;
            var body = msgResult.Body.FromUtf8Bytes().FromJson<T>();

            var message = new Message<T>(body)
            {
                Id = Guid.Parse(props.MessageId),
                CreatedDate = ((int) props.Timestamp.UnixTime).FromUnixTime(),
                Priority = props.Priority,
                ReplyTo = props.ReplyTo,
                Tag = msgResult.DeliveryTag.ToString(),
                RetryAttempts = msgResult.Redelivered ? 1 : 0,
            };

            if (props.CorrelationId != null)
            {
                message.ReplyId = Guid.Parse(props.CorrelationId);
            }

            if (props.Headers != null)
            {
                object errors = null;
                props.Headers.TryGetValue("Error", out errors);
                if (errors != null)
                {
                    var errorsJson = (string)errors;
                    message.Error = errorsJson.FromJson<MessageError>();
                }
            }

            return message;
        }
    }
}