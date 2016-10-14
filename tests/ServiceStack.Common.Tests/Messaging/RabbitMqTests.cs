#if !NETCORE_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Messaging
{
    public class HelloRabbit
    {
        public string Name { get; set; }
    }

    [TestFixture, Explicit]
    public class RabbitMqTests
    {
        private readonly ConnectionFactory mqFactory = new ConnectionFactory { HostName = "localhost" };
        private const string Exchange = "mq:tests";
        private const string ExchangeDlq = "mq:tests.dlq";
        private const string ExchangeTopic = "mq:tests.topic";
        private const string ExchangeFanout = "mq:tests.fanout";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.RegisterDirectExchange(Exchange);
                channel.RegisterDlqExchange(ExchangeDlq);
                channel.RegisterTopicExchange(ExchangeTopic);

                RegisterQueue(channel, QueueNames<HelloRabbit>.In);
                RegisterQueue(channel, QueueNames<HelloRabbit>.Priority);
                RegisterDlq(channel, QueueNames<HelloRabbit>.Dlq);
                RegisterTopic(channel, QueueNames<HelloRabbit>.Out);
                RegisterQueue(channel, QueueNames<HelloRabbit>.In, exchange: ExchangeTopic);

                channel.PurgeQueue<HelloRabbit>();
            }
        }

        public static void RegisterQueue(IModel channel, string queueName, string exchange = Exchange)
        {
            var args = new Dictionary<string, object> {
                {"x-dead-letter-exchange", ExchangeDlq },
                {"x-dead-letter-routing-key", queueName.Replace(".inq",".dlq").Replace(".priorityq",".dlq") },
            };
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueBind(queueName, exchange, routingKey: queueName);
        }

        public static void RegisterTopic(IModel channel, string queueName)
        {
            channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queueName, ExchangeTopic, routingKey: queueName);
        }

        public static void RegisterDlq(IModel channel, string queueName)
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queueName, ExchangeDlq, routingKey: queueName);
        }

        public void ExchangeDelete(IModel channel, string exchange)
        {
            try
            {
                channel.ExchangeDelete(exchange);
            }
            catch (Exception ex)
            {
                "Error ExchangeDelete(): {0}".Print(ex.Message);
            }
        }

        [Test]
        public void Can_publish_messages_to_RabbitMQ()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                5.Times(i =>
                {
                    byte[] payload = new HelloRabbit { Name = "World! #{0}".Fmt(i) }.ToJson().ToUtf8Bytes();
                    var props = channel.CreateBasicProperties();
                    props.Persistent = true;

                    channel.BasicPublish(exchange: Exchange,
                        routingKey: QueueNames<HelloRabbit>.In, basicProperties: props, body: payload);

                    Console.WriteLine("Sent Message " + i);
                    Thread.Sleep(1000);
                });
            }
        }

        [Test]
        public void Can_consume_messages_from_RabbitMQ_with_BasicGet()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                PublishHelloRabbit(channel);

                while (true)
                {
                    var basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.In, noAck: false);

                    if (basicGetMsg == null)
                    {
                        "End of the road...".Print();
                        return;
                    }

                    var msg = basicGetMsg.Body.FromUtf8Bytes().FromJson<HelloRabbit>();

                    Thread.Sleep(1000);

                    channel.BasicAck(basicGetMsg.DeliveryTag, multiple: false);
                }
            }
        }

        [Test]
        public void Can_consume_messages_from_RabbitMQ_with_BasicConsume()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                var consumerTag = channel.BasicConsume(QueueNames<HelloRabbit>.In, noAck: false, consumer: consumer);
                string recvMsg = null;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(100);
                    PublishHelloRabbit(channel);
                });

                while (true)
                {
                    try
                    {
                        var e = consumer.Queue.Dequeue();
                        "Dequeued".Print();

                        var props = e.BasicProperties;
                        recvMsg = e.Body.FromUtf8Bytes();
                        // ... process the message
                        recvMsg.Print();

                        channel.BasicAck(e.DeliveryTag, multiple: false);
                        break;
                    }
                    catch (OperationInterruptedException ex)
                    {
                        // The consumer was removed, either through
                        // channel or connection closure, or through the
                        // action of IModel.BasicCancel().
                        "End of the road...".Print();
                        break;
                    }
                }

                Assert.That(recvMsg, Is.Not.Null);
            }
        }

        [Test]
        public void Publishing_message_with_routingKey_sends_only_to_registered_queue()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                PublishHelloRabbit(channel);

                var basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.In, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);

                basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.Priority, noAck: true);
                Assert.That(basicGetMsg, Is.Null);
            }
        }

        private static void PublishHelloRabbit(IModel channel, string text = "World!")
        {
            byte[] payload = new HelloRabbit { Name = text }.ToJson().ToUtf8Bytes();
            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            channel.BasicPublish(Exchange, QueueNames<HelloRabbit>.In, props, payload);
        }

        [Test]
        public void Publishing_message_to_fanout_exchange_publishes_to_all_queues()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.RegisterFanoutExchange(ExchangeFanout);

                RegisterQueue(channel, QueueNames<HelloRabbit>.In, exchange: ExchangeFanout);
                RegisterQueue(channel, QueueNames<HelloRabbit>.Priority, exchange: ExchangeFanout);

                byte[] payload = new HelloRabbit { Name = "World!" }.ToJson().ToUtf8Bytes();
                var props = channel.CreateBasicProperties();
                props.Persistent = true;

                channel.BasicPublish(ExchangeFanout, QueueNames<HelloRabbit>.In, props, payload);

                var basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.In, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);

                basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.Priority, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);
            }
        }

        [Test]
        public void Does_publish_to_dead_letter_exchange()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.OpenChannel())
            {
                PublishHelloRabbit(channel);

                var basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.In, noAck: true);
                var dlqBasicMsg = channel.BasicGet(QueueNames<HelloRabbit>.Dlq, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);
                Assert.That(dlqBasicMsg, Is.Null);

                PublishHelloRabbit(channel);

                basicGetMsg = channel.BasicGet(QueueNames<HelloRabbit>.In, noAck: false);
                Thread.Sleep(500);
                dlqBasicMsg = channel.BasicGet(QueueNames<HelloRabbit>.Dlq, noAck: false);
                Assert.That(basicGetMsg, Is.Not.Null);
                Assert.That(dlqBasicMsg, Is.Null);

                channel.BasicNack(basicGetMsg.DeliveryTag, multiple: false, requeue: false);

                Thread.Sleep(500);
                dlqBasicMsg = channel.BasicGet(QueueNames<HelloRabbit>.Dlq, noAck: true);
                Assert.That(dlqBasicMsg, Is.Not.Null);
            }
        }

        [Test]
        public void Can_interrupt_BasicConsumer_in_bgthread_by_closing_channel()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                string recvMsg = null;
                EndOfStreamException lastEx = null;

                var bgThread = new Thread(() =>
                {
                    try
                    {
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(QueueNames<HelloRabbit>.In, noAck: false, consumer: consumer);

                        while (true)
                        {
                            try
                            {
                                var e = consumer.Queue.Dequeue();
                                recvMsg = e.Body.FromUtf8Bytes();
                            }
                            catch (EndOfStreamException ex)
                            {
                                // The consumer was cancelled, the model closed, or the
                                // connection went away.
                                "EndOfStreamException in bgthread: {0}".Print(ex.Message);
                                lastEx = ex;
                                return;
                            }
                            catch (Exception ex)
                            {
                                Assert.Fail("Unexpected exception in bgthread: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        "Exception in bgthread: {0}: {1}".Print(ex.GetType().Name, ex.Message);
                    }
                })
                {
                    Name = "Closing Channel Test",
                    IsBackground = true,
                };
                bgThread.Start();

                PublishHelloRabbit(channel);
                Thread.Sleep(100);

                //closing either throws EndOfStreamException in bgthread
                channel.Close();
                //connection.Close();

                Thread.Sleep(2000);

                Assert.That(recvMsg, Is.Not.Null);
                Assert.That(lastEx, Is.Not.Null);

                "EOF...".Print();
            }
        }

        [Test]
        public void Can_consume_messages_with_BasicConsumer()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                OperationInterruptedException lastEx = null;

                channel.Close();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        PublishHelloRabbit(channel);
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex as OperationInterruptedException;
                        "Caught {0}: {1}".Print(ex.GetType().Name, ex);
                    }
                });

                Thread.Sleep(1000);

                Assert.That(lastEx, Is.Not.Null);

                "EOF...".Print();
            }
        }

        [Test]
        public void Delete_all_queues_and_exchanges()
        {
            var exchangeNames = new[] {
                Exchange,
                ExchangeDlq,
                ExchangeTopic,
                ExchangeFanout,
                QueueNames.Exchange,
                QueueNames.ExchangeDlq,
                QueueNames.ExchangeTopic,
            };

            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                exchangeNames.Each(x => channel.ExchangeDelete(x));

                channel.DeleteQueue<AlwaysThrows>();
                channel.DeleteQueue<Hello>();
                channel.DeleteQueue<HelloRabbit>();
                channel.DeleteQueue<HelloResponse>();
                channel.DeleteQueue<Incr>();
                channel.DeleteQueue<AnyTestMq>();
                channel.DeleteQueue<AnyTestMqResponse>();
                channel.DeleteQueue<PostTestMq>();
                channel.DeleteQueue<PostTestMqResponse>();
                channel.DeleteQueue<ValidateTestMq>();
                channel.DeleteQueue<ValidateTestMqResponse>();
                channel.DeleteQueue<ThrowGenericError>();
                channel.DeleteQueue<Reverse>();
                channel.DeleteQueue<Rot13>();
                channel.DeleteQueue<Wait>();
            }
        }
    }

    //Dummy messages to delete Queue's created else where.
    public class AlwaysThrows { }
    public class Hello { }
    public class HelloResponse { }
    public class Reverse { }
    public class Rot13 { }
    public class Wait { }

}
#endif