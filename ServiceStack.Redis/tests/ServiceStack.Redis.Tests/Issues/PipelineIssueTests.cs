using System;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Issues
{
    [TestFixture]
    public class PipelineIssueTests
        : RedisClientTestsBase
    {
        [Test]
        public void Disposing_Client_Clears_Pipeline()
        {
            var clientMgr = new PooledRedisClientManager(TestConfig.SingleHost);

            using (var client = clientMgr.GetClient())
            {
                client.Set("k1", "v1");
                client.Set("k2", "v2");
                client.Set("k3", "v3");
                
                using (var pipe = client.CreatePipeline())
                {
                    pipe.QueueCommand(c => c.Get<string>("k1"), p => { throw new Exception(); });
                    pipe.QueueCommand(c => c.Get<string>("k2"));

                    try
                    {
                        pipe.Flush();
                    }
                    catch (Exception)
                    {
                        //The exception is expected. Swallow it.
                    }
                }
            }

            using (var client = clientMgr.GetClient())
            {
                Assert.AreEqual("v3", client.Get<string>("k3"));
            }
        }

        [Test]
        public void Can_Set_with_DateTime_in_Pipeline()
        {
            using (var clientsManager = new RedisManagerPool(TestConfig.SingleHost))
            {
                bool result;
                int value = 111;
                string key = $"key:{value}";

                // Set key with pipeline (batching many requests)
                using (var redis = clientsManager.GetClient())
                {
                    using (var pipeline = redis.CreatePipeline())
                    {
                        //Only atomic operations can be called within a Transaction or Pipeline
                        Assert.Throws<NotSupportedException>(() =>
                            pipeline.QueueCommand(r => r.Set(key, value, DateTime.Now.AddMinutes(1)), r => result = r));
                    }

                    using (var pipeline = redis.CreatePipeline())
                    {
                        pipeline.QueueCommand(r => r.Set(key, value), r => result = r);
                        pipeline.QueueCommand(r => r.ExpireEntryAt(key, DateTime.Now.AddMinutes(1)));
                        
                        pipeline.Flush();
                    }
                }

                // Get key
                using (var redis = clientsManager.GetClient())
                {
                    var res = redis.Get<int>(key);                    
                    Assert.That(res, Is.EqualTo(value));
                }
            }
            
        }
    }
}