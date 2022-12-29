using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    public class SqsMqRequestReplyTests : MqRequestReplyTests
    {
        private SqsQueueManager sqsQueueManager;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            sqsQueueManager = new SqsQueueManager(SqsTestClientFactory.GetConnectionFactory());
            LogManager.LogFactory = new ConsoleLogFactory();
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {   // Cleanup anything left cached that we tested with
            if (SqsTestAssert.IsFakeClient)
            {
                return;
            }

            var queueNamesToDelete = new List<string>(sqsQueueManager.QueueNameMap.Keys);
            foreach (var queueName in queueNamesToDelete)
            {
                try
                {
                    sqsQueueManager.DeleteQueue(queueName);
                }
                catch { }
            }
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new SqsMqServer(new SqsMqMessageFactory(sqsQueueManager))
            {
                DisableBuffering = true,
                RetryCount = retryCount
            };
        }
    }

    public class HelloIntro : IReturn<HelloIntroResponse>
    {
        public string Name { get; set; }
    }

    public class HelloIntroResponse
    {
        public string Result { get; set; }
    }

    public class HelloService : Service
    {
        public object Any(HelloIntro request)
        {
            return new HelloIntroResponse { Result = $"Hello, {request.Name}!"};
        }
    }

    [Explicit("Integration Tests")]
    [TestFixture]
    public abstract class MqRequestReplyTests
    {
        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [Test]
        public void Can_publish_messages_to_the_ReplyTo_temporary_queue()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new HelloIntroResponse { Result = $"Hello, {m.GetBody().Name}!"});
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyToMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Can_send_message_with_custom_Header()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new Message<HelloIntroResponse>(new HelloIntroResponse {
                        Result = $"Hello, {m.GetBody().Name}!"
                    }) {
                        Meta = m.Meta
                    });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyToMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq,
                        Meta = new Dictionary<string, string> { { "Custom", "Header" } }
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                    Assert.That(responseMsg.Meta["Custom"], Is.EqualTo("Header"));
                }
            }
        }

        public class Incr
        {
            public long Value { get; set; }
        }

        public class IncrResponse
        {
            public long Result { get; set; }
        }

        [Test]
        public void Can_handle_multiple_rpc_clients()
        {
            int NoOfClients = 10;
            int TimeMs = 5000;

            var errors = new ConcurrentDictionary<long, string>();
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<Incr>(m =>
                    new IncrResponse { Result = m.GetBody().Value + 1 });
                mqServer.Start();

                long counter = 0;
                int activeClients = 0;
                var activeClientsLock = new object();

                NoOfClients.Times(() => {
                    ThreadPool.QueueUserWorkItem(_ => {
                        using (var mqClient = mqServer.CreateMessageQueueClient())
                        {
                            var sw = Stopwatch.StartNew();
                            var clientId = Interlocked.Increment(ref activeClients);
                            while (sw.ElapsedMilliseconds < TimeMs)
                            {
                                var next = Interlocked.Increment(ref counter);
                                try
                                {
                                    var replyToMq = mqClient.GetTempQueueName();
                                    mqClient.Publish(new Message<Incr>(new Incr { Value = next })
                                    {
                                        ReplyTo = replyToMq
                                    });

                                    var responseMsg = mqClient.Get<IncrResponse>(replyToMq, TimeSpan.FromMilliseconds(TimeMs));
                                    mqClient.Ack(responseMsg);

                                    var actual = responseMsg.GetBody().Result;
                                    var expected = next + 1;
                                    if (actual != expected)
                                    {
                                        errors[next] = $"Actual: {expected}, Expected: {actual}";
                                    }

                                }
                                catch (Exception ex)
                                {
                                    errors[next] = ex.Message + "\nStackTrace:\n" + ex.StackTrace;
                                }
                            }

                            "Client {0} finished".Print(clientId);
                            if (Interlocked.Decrement(ref activeClients) == 0)
                            {
                                "All Clients Finished".Print();
                                lock (activeClientsLock)
                                    Monitor.Pulse(activeClientsLock);
                            }
                        }
                    });
                });

                lock (activeClientsLock)
                    Monitor.Wait(activeClientsLock);

                "Stopping Server...".Print();
                "Requests: {0}".Print(counter);
            }

            Assert.That(errors.Count, Is.EqualTo(0));
        }
    }
}