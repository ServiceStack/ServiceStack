using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture, Category("Integration")]
    public class SqsMqServerTests
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

            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<Reverse>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<Rot13>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<AlwaysThrows>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<Incr2>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<Wait>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<Hello>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<HelloResponse>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<HelloOut>.AllQueueNames);
            }
            catch { }
            try
            {
                sqsQueueManager.PurgeQueues(QueueNames<HelloReply>.AllQueueNames);
            }
            catch { }
        }

        private class Reverse
        {
            public string Value { get; set; }
        }

        private class Rot13
        {
            public string Value { get; set; }
        }

        private SqsMqServer CreateMqServer(int noOfRetries = 2)
        {
            return new SqsMqServer(new SqsMqMessageFactory(sqsQueueManager))
            {
                DisableBuffering = true,
                RetryCount = noOfRetries
            };
        }

        private void Publish_4_messages(IMessageQueueClient mqClient)
        {
            mqClient.Publish(new Reverse
            {
                Value = "Hello"
            });
            mqClient.Publish(new Reverse
            {
                Value = "World"
            });
            mqClient.Publish(new Reverse
            {
                Value = "ServiceStack"
            });
            mqClient.Publish(new Reverse
            {
                Value = "Redis"
            });
        }

        private void Publish_4_Rot13_messages(IMessageQueueClient mqClient)
        {
            mqClient.Publish(new Rot13
            {
                Value = "Hello"
            });
            mqClient.Publish(new Rot13
            {
                Value = "World"
            });
            mqClient.Publish(new Rot13
            {
                Value = "ServiceStack"
            });
            mqClient.Publish(new Rot13
            {
                Value = "Redis"
            });
        }

        [Test]
        public void Utils_publish_Reverse_messages()
        {
            var mqHost = CreateMqServer();
            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();
            Publish_4_messages(mqClient);
            mqHost.Stop();
        }

        [Test]
        public void Utils_publish_Rot13_messages()
        {
            var mqHost = CreateMqServer();
            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();
            Publish_4_Rot13_messages(mqClient);
            mqHost.Stop();
        }

        [Test]
        public void Does_process_messages_sent_before_it_was_started()
        {
            var reverseCalled = 0;

            var mqHost = CreateMqServer();
            mqHost.RegisterHandler<Reverse>(x =>
            {
                Interlocked.Increment(ref reverseCalled);
                return x.GetBody().Value.Reverse();
            });

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();
            Publish_4_messages(mqClient);

            mqHost.Start();

            SqsTestAssert.WaitUntilTrueOrTimeout(() => reverseCalled >= 4, timeoutSeconds: 3);

            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(4));
            Assert.That(reverseCalled, Is.EqualTo(4));

            mqHost.Dispose();
        }

        [Test]
        public void Does_process_all_messages_and_Starts_Stops_correctly_with_multiple_threads_racing()
        {
            var mqHost = CreateMqServer();

            var reverseCalled = 0;
            var rot13Called = 0;

            mqHost.RegisterHandler<Reverse>(x =>
            {
                Interlocked.Increment(ref reverseCalled);
                "Processing Reverse {0}...".Print(reverseCalled);
                return x.GetBody().Value.Reverse();
            });
            mqHost.RegisterHandler<Rot13>(x =>
            {
                Interlocked.Increment(ref rot13Called);
                "Processing Rot13 {0}...".Print(rot13Called);
                return x.GetBody().Value.ToRot13();
            });

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();
            mqClient.Publish(new Reverse
            {
                Value = "Hello"
            });
            mqClient.Publish(new Reverse
            {
                Value = "World"
            });
            mqClient.Publish(new Rot13
            {
                Value = "ServiceStack"
            });

            mqHost.Start();

            SqsTestAssert.WaitUntilTrueOrTimeout(() => reverseCalled >= 2 && rot13Called >= 1, timeoutSeconds: 4);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(3));

            mqClient.Publish(new Reverse
            {
                Value = "Foo"
            });
            mqClient.Publish(new Rot13
            {
                Value = "Bar"
            });

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
            {
                var status = mqHost.GetStatus();
                return status != null && status.Equals("Stopped");
            }, timeoutSeconds: 2);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
                                                 {
                                                     var status = mqHost.GetStatus();
                                                     return reverseCalled >= 3 && rot13Called >= 2 && status != null && status.Equals("Started");
                                                 }, timeoutSeconds: 4);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            Debug.WriteLine("\n" + mqHost.GetStats());

            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(5));
            Assert.That(reverseCalled, Is.EqualTo(3));
            Assert.That(rot13Called, Is.EqualTo(2));

            mqHost.Dispose();
        }

        [Test]
        public void Only_allows_1_BgThread_to_run_at_a_time()
        {
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.RegisterHandler<Rot13>(x => x.GetBody().Value.ToRot13());

            5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
            {
                var status = mqHost.GetStatus();
                return status != null && status.Equals("Started");
            }, timeoutSeconds: 1);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
            Assert.That(mqHost.BgThreadCount, Is.EqualTo(1));

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
            {
                var status = mqHost.GetStatus();
                return status != null && status.Equals("Stopped");
            }, timeoutSeconds: 1);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));

            ThreadPool.QueueUserWorkItem(y => mqHost.Start());

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
            {
                var status = mqHost.GetStatus();
                return status != null && status.Equals("Started");
            }, timeoutSeconds: 1);

            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            Assert.That(mqHost.BgThreadCount, Is.EqualTo(2));

            Debug.WriteLine(mqHost.GetStats());

            mqHost.Dispose();
        }

        [Test]
        public void Cannot_Start_a_Disposed_MqHost()
        {
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.Dispose();

            Assert.Throws<ObjectDisposedException>(mqHost.Start);
        }

        [Test]
        public void Cannot_Stop_a_Disposed_MqHost()
        {
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.Start();

            SqsTestAssert.WaitUntilTrueOrTimeout(() =>
            {
                var status = mqHost.GetStatus();
                return status != null && status.Equals("Started");
            }, timeoutSeconds: 2);

            mqHost.Dispose();

            Assert.Throws<ObjectDisposedException>(mqHost.Stop);
        }

        public class AlwaysThrows
        {
            public string Value { get; set; }
        }

        [Test]
        public void Does_retry_messages_with_errors_by_RetryCount()
        {
            const int retryCount = 1;
            const int totalRetries = 1 + retryCount;

            var mqHost = CreateMqServer(retryCount);

            var reverseCalled = 0;
            var rot13Called = 0;
            var alwaysThrowsCalled = 0;

            mqHost.RegisterHandler<Reverse>(x =>
            {
                Interlocked.Increment(ref reverseCalled);
                return x.GetBody().Value.Reverse();
            });
            mqHost.RegisterHandler<Rot13>(x =>
            {
                Interlocked.Increment(ref rot13Called);
                return x.GetBody().Value.ToRot13();
            });
            mqHost.RegisterHandler<AlwaysThrows>(x =>
            {
                Interlocked.Increment(ref alwaysThrowsCalled);
                throw new Exception("Always Throwing! " + x.GetBody().Value);
            });

            mqHost.Start();

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();
            mqClient.Publish(new AlwaysThrows
            {
                Value = "1st"
            });
            mqClient.Publish(new Reverse
            {
                Value = "Hello"
            });
            mqClient.Publish(new Reverse
            {
                Value = "World"
            });
            mqClient.Publish(new Rot13
            {
                Value = "ServiceStack"
            });

            SqsTestAssert.WaitUntilTrueOrTimeout(() => reverseCalled >= 2 && rot13Called >= 1 && alwaysThrowsCalled >= 1);

            Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo(1 * totalRetries));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(2 + 1));

            5.Times(x => mqClient.Publish(new AlwaysThrows
            {
                Value = "#" + x
            }));

            mqClient.Publish(new Reverse
            {
                Value = "Hello"
            });
            mqClient.Publish(new Reverse
            {
                Value = "World"
            });
            mqClient.Publish(new Rot13
            {
                Value = "ServiceStack"
            });

            SqsTestAssert.WaitUntilTrueOrTimeout(() => reverseCalled >= 4 && rot13Called >= 2 && alwaysThrowsCalled >= 6, timeoutSeconds: 5);

            Debug.WriteLine(mqHost.GetStatsDescription());

            Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo((1 + 5) * totalRetries));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(6));

            Assert.That(reverseCalled, Is.EqualTo(2 + 2));
            Assert.That(rot13Called, Is.EqualTo(1 + 1));
        }

        public class Incr2
        {
            public int Value { get; set; }
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses()
        {
            var called = 0;
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Incr2>(m =>
            {
                Debug.WriteLine("In Incr #" + m.GetBody().Value);
                called++;
                return m.GetBody().Value > 0
                    ? new Incr2 { Value = m.GetBody().Value - 1 }
                    : null;
            });

            mqHost.Start();

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();

            var incr = new Incr2
            {
                Value = 5
            };
            mqClient.Publish(incr);

            SqsTestAssert.WaitUntilTrueOrTimeout(() => called >= incr.Value + 1);

            Assert.That(called, Is.EqualTo(1 + incr.Value));
        }

        private class Hello
        {
            public string Name { get; set; }
        }

        private class HelloResponse
        {
            public string Result { get; set; }
        }

        [Test]
        public void Can_receive_and_process_standard_request_reply_combo()
        {
            var mqHost = CreateMqServer();

            string messageReceived = null;

            mqHost.RegisterHandler<Hello>(m => new HelloResponse
            {
                Result = "Hello, " + m.GetBody().Name
            });

            mqHost.RegisterHandler<HelloResponse>(m =>
            {
                messageReceived = m.GetBody().Result;
                return null;
            });

            mqHost.Start();

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();

            var dto = new Hello
            {
                Name = "ServiceStack"
            };
            mqClient.Publish(dto);

            SqsTestAssert.WaitUntilTrueOrTimeout(() => messageReceived != null &&
                                         messageReceived.Equals("Hello, ServiceStack"),
                                   timeoutSeconds: 12);

            Assert.That(messageReceived, Is.EqualTo("Hello, ServiceStack"));
        }

        private class Wait
        {
            public int ForMs { get; set; }
        }

        [Test]
        public void Can_handle_requests_concurrently_in_2_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 2, msgs: 10);
        }

        [Test]
        public void Can_handle_requests_concurrently_in_3_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 3, msgs: 10);
        }

        [Test]
        public void Can_handle_requests_concurrently_in_4_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 4, msgs: 10);
        }

        private void RunHandlerOnMultipleThreads(int noOfThreads, int msgs)
        {
            var timesCalled = 0;
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Wait>(m =>
            {
                Interlocked.Increment(ref timesCalled);
                Thread.Sleep(m.GetBody().ForMs);
                return null;
            }, noOfThreads);

            mqHost.Start();

            var mqClient = mqHost.MessageFactory.CreateMessageQueueClient();

            var dto = new Wait
            {
                ForMs = 100
            };
            msgs.Times(i => mqClient.Publish(dto));

            SqsTestAssert.WaitUntilTrueOrTimeout(() => timesCalled >= msgs, timeoutSeconds: 12);

            mqHost.Dispose();

            Assert.That(timesCalled, Is.EqualTo(msgs));
        }

        [Test]
        public void Can_publish_and_receive_messages_with_MessageFactory()
        {
            using (var mqFactory = new SqsMqMessageFactory(sqsQueueManager))
            {
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello
                    {
                        Name = "Foo"
                    });

                    var msg = mqClient.Get<Hello>(QueueNames<Hello>.In, TimeSpan.FromSeconds(30));

                    Assert.IsNotNull(msg, "Should have received msg from IN queue but did not");
                    Assert.That(msg.GetBody().Name, Is.EqualTo("Foo"));
                }
            }
        }

        private class HelloOut
        {
            public string Name { get; set; }
        }

        [Test]
        public void Messages_with_null_Response_is_published_to_OutMQ()
        {
            var msgsReceived = 0;

            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloOut>(m =>
                {
                    Interlocked.Increment(ref msgsReceived);
                    return null;
                });

                mqServer.Start();

                using (var mqClient = mqServer.MessageFactory.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloOut
                    {
                        Name = "Into the Void"
                    });

                    var msg = mqClient.Get<HelloOut>(QueueNames<HelloOut>.Out, TimeSpan.FromSeconds(30));

                    Assert.IsNotNull(msg, "Should have received msg from OUT queue but did not");

                    var response = msg.GetBody();

                    SqsTestAssert.WaitUntilTrueOrTimeout(() => msgsReceived >= 1, timeoutSeconds: 1);

                    Assert.That(response.Name, Is.EqualTo("Into the Void"));
                    Assert.That(msgsReceived, Is.EqualTo(1));
                }
            }

        }

        private class HelloReply
        {
            public string Name { get; set; }
        }

        [Test]
        public void Messages_with_null_Response_is_published_to_ReplyMQ()
        {
            var msgsReceived = 0;

            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloReply>(m =>
                {
                    Interlocked.Increment(ref msgsReceived);
                    return null;
                });

                mqServer.Start();

                using (var mqClient = mqServer.MessageFactory.CreateMessageQueueClient())
                {
                    var replyMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloReply>(new HelloReply
                    {
                        Name = "Into the Void"
                    })
                    {
                        ReplyTo = replyMq
                    });

                    var msg = mqClient.Get<HelloReply>(replyMq, TimeSpan.FromSeconds(30));

                    Assert.IsNotNull(msg, "Should have received msg from REPLY queue but did not");

                    var response = msg.GetBody();

                    SqsTestAssert.WaitUntilTrueOrTimeout(() => msgsReceived >= 1, timeoutSeconds: 1); ;

                    Assert.That(response.Name, Is.EqualTo("Into the Void"));
                    Assert.That(msgsReceived, Is.EqualTo(1));
                }
            }
        }
    }
}
