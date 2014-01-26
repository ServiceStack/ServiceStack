using System;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Messaging
{
    public class Hello
    {
        public string Name { get; set; }
    }

    [TestFixture, Explicit]
    public class RabbitMqTests
    {
        private readonly ConnectionFactory mqFactory = new ConnectionFactory { HostName = "localhost" };
        private const string ExchangeName = "mq:exchange";

        [Test]
        public void Can_publish_messages_to_RabbitMQ()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(ExchangeName, "direct");
                channel.QueueDeclare(QueueNames<Hello>.In, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueNames<Hello>.In, ExchangeName, routingKey: QueueNames<Hello>.In);

                for (int i = 0; i < 100; i++)
                {
                    byte[] payload = new Hello { Name = "World! #{0}".Fmt(i) }.ToJson().ToUtf8Bytes();
                    var props = channel.CreateBasicProperties();
                    props.SetPersistent(true);

                    channel.BasicPublish(exchange: ExchangeName,
                        routingKey: QueueNames<Hello>.In, basicProperties: props, body: payload);

                    Console.WriteLine("Sent Message " + i);
                    Thread.Sleep(1000);
                }
            }
        }

        [Test]
        public void Can_consume_messages_from_RabbitMQ_with_BasicGet()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(ExchangeName, "direct");
                channel.QueueDeclare(QueueNames<Hello>.In, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueNames<Hello>.In, ExchangeName, routingKey: QueueNames<Hello>.In);

                while (true)
                {
                    var basicGetMsg = channel.BasicGet(QueueNames<Hello>.In, noAck: false);

                    if (basicGetMsg == null)
                    {
                        "End of the road...".Print();
                        return;
                    }

                    var msg = basicGetMsg.Body.FromUtf8Bytes().FromJson<Hello>();

                    msg.PrintDump();

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
                channel.ExchangeDeclare(ExchangeName, "direct");
                channel.QueueDeclare(QueueNames<Hello>.In, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueNames<Hello>.In, ExchangeName, routingKey: QueueNames<Hello>.In);

                var consumer = new QueueingBasicConsumer(channel);
                var consumerTag = channel.BasicConsume(QueueNames<Hello>.In, noAck: false, consumer: consumer);
                while (true)
                {
                    try
                    {
                        var e = consumer.Queue.Dequeue();
                        "Dequeued".Print();

                        var props = e.BasicProperties;
                        var msg = e.Body.FromUtf8Bytes().FromJson<Hello>();
                        // ... process the message
                        msg.PrintDump();

                        Thread.Sleep(1000);

                        channel.BasicAck(e.DeliveryTag, multiple: false);
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
            }
        }

        [Test]
        public void Publishing_message_with_routingKey_sends_only_to_registered_queue()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.RegisterDirectExchange(QueueNames.Exchange);
                channel.RegisterQueue(QueueNames<Hello>.In);
                channel.RegisterQueue(QueueNames<Hello>.Priority);

                byte[] payload = new Hello { Name = "World!" }.ToJson().ToUtf8Bytes();
                var props = channel.CreateBasicProperties();
                props.SetPersistent(true);

                channel.BasicPublish(QueueNames.Exchange, QueueNames<Hello>.In, props, payload);

                var basicGetMsg = channel.BasicGet(QueueNames<Hello>.In, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);

                basicGetMsg = channel.BasicGet(QueueNames<Hello>.Priority, noAck: true);
                Assert.That(basicGetMsg, Is.Null);
            }
        }

        [Test]
        public void Publishing_message_to_fanout_exchange_publishes_to_all_queues()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(QueueNames.ExchangeTopic, "fanout");

                channel.QueueDeclare(QueueNames<Hello>.In, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueNames<Hello>.In, QueueNames.ExchangeTopic, routingKey: QueueNames<Hello>.In);

                channel.QueueDeclare(QueueNames<Hello>.Priority, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueNames<Hello>.Priority, QueueNames.ExchangeTopic, routingKey: QueueNames<Hello>.Priority);

                byte[] payload = new Hello { Name = "World!" }.ToJson().ToUtf8Bytes();
                var props = channel.CreateBasicProperties();
                props.SetPersistent(true);

                channel.BasicPublish(QueueNames.ExchangeTopic, QueueNames<Hello>.In, props, payload);

                var basicGetMsg = channel.BasicGet(QueueNames<Hello>.In, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);

                basicGetMsg = channel.BasicGet(QueueNames<Hello>.Priority, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);
            }
        }

        [Test]
        public void Does_publish_to_dead_letter_exchange()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.OpenChannel())
            {
                channel.RegisterQueue(QueueNames<Hello>.In);
                channel.RegisterDlq(QueueNames<Hello>.Dlq);

                byte[] payload = new Hello { Name = "World!" }.ToJson().ToUtf8Bytes();
                var props = channel.CreateBasicProperties();
                props.SetPersistent(true);

                channel.BasicPublish(QueueNames.Exchange, QueueNames<Hello>.In, props, payload);

                var basicGetMsg = channel.BasicGet(QueueNames<Hello>.In, noAck: true);
                var dlqBasicMsg = channel.BasicGet(QueueNames<Hello>.Dlq, noAck: true);
                Assert.That(basicGetMsg, Is.Not.Null);
                Assert.That(dlqBasicMsg, Is.Null);

                channel.BasicPublish(QueueNames.Exchange, QueueNames<Hello>.In, props, payload);

                basicGetMsg = channel.BasicGet(QueueNames<Hello>.In, noAck: false);
                Thread.Sleep(500);
                dlqBasicMsg = channel.BasicGet(QueueNames<Hello>.Dlq, noAck: false);
                Assert.That(basicGetMsg, Is.Not.Null);
                Assert.That(dlqBasicMsg, Is.Null);

                channel.BasicNack(basicGetMsg.DeliveryTag, multiple: false, requeue: false);

                Thread.Sleep(500);
                dlqBasicMsg = channel.BasicGet(QueueNames<Hello>.Dlq, noAck: true);
                Assert.That(dlqBasicMsg, Is.Not.Null);
            }
        }

        [Test]
        public void Can_interrupt_BasicConsumer_in_bgthread_by_closing_channel()
        {
            using (IConnection connection = mqFactory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.RegisterDirectExchange(QueueNames.Exchange);
                channel.RegisterQueue(QueueNames<Hello>.In);

                string recvMsg = null;
                EndOfStreamException lastEx = null;

                var bgThread = new Thread(() => {
                    try
                    {
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(QueueNames<Hello>.In, noAck: false, consumer: consumer);

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
                        "Exception in bgthread: {0}: {1}".Print(ex.GetType().Name,ex.Message);
                    }
                })
                {
                    Name = "Closing Channel Test",
                    IsBackground = true,
                };
                bgThread.Start();

                byte[] payload = new Hello { Name = "World!" }.ToJson().ToUtf8Bytes();
                var props = channel.CreateBasicProperties();
                props.SetPersistent(true);

                channel.BasicPublish(QueueNames.Exchange, QueueNames<Hello>.In, props, payload);

                //closing either throws EndOfStreamException in bgthread
                channel.Close();
                //connection.Close();

                Thread.Sleep(3000);

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
                channel.RegisterDirectExchange(QueueNames.Exchange);
                channel.RegisterQueue(QueueNames<Hello>.In);
                OperationInterruptedException lastEx = null;

                channel.Close();

                ThreadPool.QueueUserWorkItem(_ => {
                    try
                    {
                        byte[] payload = new Hello { Name = "World!" }.ToJson().ToUtf8Bytes();
                        var props = channel.CreateBasicProperties();
                        props.SetPersistent(true);

                        channel.BasicPublish(QueueNames.Exchange, QueueNames<Hello>.In, props, payload);
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


    }
}