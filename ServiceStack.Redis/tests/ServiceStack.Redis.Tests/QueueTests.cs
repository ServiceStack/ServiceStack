using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Redis.Support.Queue.Implementation;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
#if NETCORE
        [Ignore(".NET Core does not implement BinaryFormatter required for these tests")]
#endif
    public class QueueTests : RedisClientTestsBase
    {
        const int numMessages = 6;
        private IList<string> messages0 = new List<string>();
        private IList<string> messages1 = new List<string>();
        private string[] patients = new[] { "patient0", "patient1" };

        [SetUp]
        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            for (int i = 0; i < numMessages; ++i)
            {
                messages0.Add(String.Format("{0}_message{1}", patients[0], i));
                messages1.Add(String.Format("{0}_message{1}", patients[1], i));
            }
        }

        [Test]
        public void TestSequentialWorkQueueUpdate()
        {
            using (var queue = new RedisSequentialWorkQueue<string>(10, 10, TestConfig.SingleHost, TestConfig.RedisPort, 1))
            {
                for (int i = 0; i < numMessages; ++i)
                {
                    queue.Enqueue(patients[0], messages0[i]);
                    queue.Enqueue(patients[1], messages1[i]);
                }

                for (int i = 0; i < numMessages / 2; ++i)
                {
                    queue.Update(patients[0], i, messages0[i] + "UPDATE");
                }
                queue.PrepareNextWorkItem();
                var batch = queue.Dequeue(numMessages / 2);
                // check that half of patient[0] messages are returned
                for (int i = 0; i < numMessages / 2; ++i)
                {
                    Assert.AreEqual(batch.DequeueItems[i], messages0[i] + "UPDATE");
                }
            }
        }

        [Test]
        public void TestSequentialWorkQueue()
        {
            using (var queue = new RedisSequentialWorkQueue<string>(10, 10, TestConfig.SingleHost, TestConfig.RedisPort, 1))
            {
                for (int i = 0; i < numMessages; ++i)
                {
                    queue.Enqueue(patients[0], messages0[i]);
                    queue.Enqueue(patients[1], messages1[i]);
                }

                queue.PrepareNextWorkItem();
                var batch = queue.Dequeue(numMessages / 2);
                // check that half of patient[0] messages are returned
                for (int i = 0; i < numMessages / 2; ++i)
                    Assert.AreEqual(batch.DequeueItems[i], messages0[i]);
                Assert.AreEqual(numMessages / 2, batch.DequeueItems.Count);
                Thread.Sleep(5000);
                Assert.IsTrue(queue.HarvestZombies());
                for (int i = 0; i < batch.DequeueItems.Count; ++i)
                    batch.DoneProcessedWorkItem();

                // check that all patient[1] messages are returned
                queue.PrepareNextWorkItem();
                batch = queue.Dequeue(2 * numMessages);
                // check that batch size is respected
                Assert.AreEqual(batch.DequeueItems.Count, numMessages);
                for (int i = 0; i < numMessages; ++i)
                {
                    Assert.AreEqual(batch.DequeueItems[i], messages1[i]);
                    batch.DoneProcessedWorkItem();
                }

                // check that there are numMessages/2 messages in the queue
                queue.PrepareNextWorkItem();
                batch = queue.Dequeue(numMessages);
                Assert.AreEqual(batch.DequeueItems.Count, numMessages / 2);

                // test pop and unlock
                batch.DoneProcessedWorkItem();
                int remaining = batch.DequeueItems.Count - 1;
                batch.PopAndUnlock();

                //process remaining items
                Assert.IsTrue(queue.PrepareNextWorkItem());
                batch = queue.Dequeue(remaining);
                Assert.AreEqual(batch.DequeueItems.Count, remaining);
                for (int i = 0; i < batch.DequeueItems.Count; ++i)
                    batch.DoneProcessedWorkItem();

                Assert.IsFalse(queue.PrepareNextWorkItem());
                batch = queue.Dequeue(remaining);
                Assert.AreEqual(batch.DequeueItems.Count, 0);
            }
        }

        [Test]
        public void TestChronologicalWorkQueue()
        {
            using (var queue = new RedisChronologicalWorkQueue<string>(10, 10, TestConfig.SingleHost, TestConfig.RedisPort))
            {
                const int numMessages = 6;
                var messages = new List<string>();
                var patients = new List<string>();
                var time = new List<double>();

                for (int i = 0; i < numMessages; ++i)
                {
                    time.Add(i);
                    patients.Add(String.Format("patient{0}", i));
                    messages.Add(String.Format("{0}_message{1}", patients[i], i));
                    queue.Enqueue(patients[i], messages[i], i);
                }

                // dequeue half of the messages
                var batch = queue.Dequeue(0, numMessages, numMessages / 2);
                // check that half of patient[0] messages are returned
                for (int i = 0; i < numMessages / 2; ++i)
                    Assert.AreEqual(batch[i].Value, messages[i]);

                // dequeue the rest of the messages
                batch = queue.Dequeue(0, numMessages, 2 * numMessages);
                // check that batch size is respected
                Assert.AreEqual(batch.Count, numMessages / 2);
                for (int i = 0; i < numMessages / 2; ++i)
                    Assert.AreEqual(batch[i].Value, messages[i + numMessages / 2]);

                // check that there are no more messages in the queue
                batch = queue.Dequeue(0, numMessages, numMessages);
                Assert.AreEqual(batch.Count, 0);
            }
        }

        [Test]
       public void TestSimpleWorkQueue()
        {
            using (var queue = new RedisSimpleWorkQueue<string>(10, 10, TestConfig.SingleHost, TestConfig.RedisPort))
            {
                int numMessages = 6;
                var messages = new string[numMessages];
                for (int i = 0; i < numMessages; ++i)
                {
                    messages[i] = String.Format("message#{0}", i);
                    queue.Enqueue(messages[i]);
                }
                var batch = queue.Dequeue(numMessages * 2);
                //test that batch size is respected
                Assert.AreEqual(batch.Count, numMessages);

                // test that messages are returned, in correct order
                for (int i = 0; i < numMessages; ++i)
                    Assert.AreEqual(messages[i], batch[i]);

                //test that messages were removed from queue
                batch = queue.Dequeue(numMessages * 2);
                Assert.AreEqual(batch.Count, 0);
            }
        }
    }
}