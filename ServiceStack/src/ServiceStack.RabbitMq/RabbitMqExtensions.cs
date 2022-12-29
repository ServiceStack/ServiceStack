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
            channel.ExchangeDeclare(exchangeName ?? QueueNames.ExchangeTopic, "topic", durable: false, autoDelete: false, arguments: null);
        }

        public static void RegisterFanoutExchange(this IModel channel, string exchangeName)
        {
            channel.ExchangeDeclare(exchangeName, "fanout", durable: false, autoDelete: false, arguments: null);
        }

        public static void RegisterQueues<T>(this IModel channel)
        {
            channel.RegisterQueue(QueueNames<T>.In);
            channel.RegisterQueue(QueueNames<T>.Priority);
            channel.RegisterTopic(QueueNames<T>.Out);
            channel.RegisterDlq(QueueNames<T>.Dlq);
        }

        public static void RegisterQueues(this IModel channel, QueueNames queueNames)
        {
            channel.RegisterQueue(queueNames.In);
            channel.RegisterQueue(queueNames.Priority);
            channel.RegisterTopic(queueNames.Out);
            channel.RegisterDlq(queueNames.Dlq);
        }

        private static RabbitMqServer GetRabbitMqServer()
        {
            if (HostContext.AppHost == null)
                return null;

            return HostContext.TryResolve<IMessageService>() as RabbitMqServer;
        }

        public static void RegisterQueue(this IModel channel, string queueName)
        {
            var args = new Dictionary<string, object> {
                {"x-dead-letter-exchange", QueueNames.ExchangeDlq },
                {"x-dead-letter-routing-key", queueName.Replace(".inq",".dlq").Replace(".priorityq",".dlq") },
            };

            GetRabbitMqServer()?.CreateQueueFilter?.Invoke(queueName, args);

            if (!QueueNames.IsTempQueue(queueName)) //Already declared in GetTempQueueName()
            {
                channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            }
            
            channel.QueueBind(queueName, QueueNames.Exchange, routingKey: queueName);
        }

        public static void RegisterDlq(this IModel channel, string queueName)
        {
            var args = new Dictionary<string, object>();

            GetRabbitMqServer()?.CreateQueueFilter?.Invoke(queueName, args);

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueBind(queueName, QueueNames.ExchangeDlq, routingKey: queueName);
        }

        public static void RegisterTopic(this IModel channel, string queueName)
        {
            var args = new Dictionary<string, object>();

            GetRabbitMqServer()?.CreateTopicFilter?.Invoke(queueName, args);

            channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueBind(queueName, QueueNames.ExchangeTopic, routingKey: queueName);
        }

        public static void DeleteQueue<T>(this IModel model)
        {
            model.DeleteQueues(QueueNames<T>.AllQueueNames);
        }

        public static void DeleteQueues(this IModel channel, params string[] queues)
        {
            foreach (var queue in queues)
            {
                try
                {
                    channel.QueueDelete(queue, ifUnused:false, ifEmpty:false);
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
            model.PurgeQueues(QueueNames<T>.AllQueueNames);
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
                    if (!ex.Is404())
                        throw;
                }
            }
        }

        public static void RegisterExchangeByName(this IModel channel, string exchange)
        {
            if (exchange.EndsWith(".dlq"))
                channel.RegisterDlqExchange(exchange);
            else if (exchange.EndsWith(".topic"))
                channel.RegisterTopicExchange(exchange);
            else 
                channel.RegisterDirectExchange(exchange);
        }

        public static void RegisterQueueByName(this IModel channel, string queueName)
        {
            if (queueName.EndsWith(".dlq"))
                channel.RegisterDlq(queueName);
            else if (queueName.EndsWith(".outq"))
                channel.RegisterTopic(queueName);
            else
                channel.RegisterQueue(queueName);
        }

        internal static bool Is404(this OperationInterruptedException ex)
        {
            return ex.Message.Contains("code=404");
        }

        public static bool IsServerNamedQueue(this string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException("queueName");
            }

            var lowerCaseQueue = queueName.ToLower();
            return lowerCaseQueue.StartsWith("amq.")
                || lowerCaseQueue.StartsWith(QueueNames.TempMqPrefix);
        }	

        public static void PopulateFromMessage(this IBasicProperties props, IMessage message)
        {
            props.MessageId = message.Id.ToString();
            props.Timestamp = new AmqpTimestamp(message.CreatedDate.ToUnixTime());
            props.Priority = (byte)message.Priority;
            props.ContentType = MimeTypes.Json;
            
            if (message.Body != null)
            {
                props.Type = message.Body.GetType().Name;
            }

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
                if (props.Headers == null)
                    props.Headers = new Dictionary<string, object>();
                props.Headers["Error"] = message.Error.ToJson();
            }
        }

        public static IMessage<T> ToMessage<T>(this BasicGetResult msgResult)
        {
            if (msgResult == null)
                return null;

            var props = msgResult.BasicProperties;
            T body;

            if (string.IsNullOrEmpty(props.ContentType) || props.ContentType.MatchesContentType(MimeTypes.Json))
            {
                var json = msgResult.Body.FromUtf8Bytes();
                body = json.FromJson<T>();
            }
            else
            {
                var deserializer = HostContext.ContentTypes.GetStreamDeserializer(props.ContentType);
                if (deserializer == null)
                    throw new NotSupportedException("Unknown Content-Type: " + props.ContentType);
            
                var ms = MemoryStreamFactory.GetStream(msgResult.Body);
                body = (T)deserializer(typeof(T), ms);
                ms.Dispose();
            }

            var message = new Message<T>(body)
            {
                Id = props.MessageId != null ? Guid.Parse(props.MessageId) : new Guid(),
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
                foreach (var entry in props.Headers)
                {
                    if (entry.Key == "Error")
                    {
                        var errors = entry.Value;
                        if (errors != null)
                        {
                            var errorsJson = errors is byte[] errorBytes
                                ? errorBytes.FromUtf8Bytes()
                                : errors.ToString();
                            message.Error = errorsJson.FromJson<ResponseStatus>();
                        }
                    }
                    else
                    {
                        if (message.Meta == null)
                            message.Meta = new Dictionary<string, string>();

                        var value = entry.Value is byte[] bytes
                            ? bytes.FromUtf8Bytes()
                            : entry.Value?.ToString();

                        message.Meta[entry.Key] = value;
                    }
                }
            }

            return message;
        }
    }
}