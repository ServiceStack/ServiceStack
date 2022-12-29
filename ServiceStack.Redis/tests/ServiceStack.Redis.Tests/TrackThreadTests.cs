using System;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    public class TrackThreadTests
    {
        [Test]
        public void Does_throw_when_using_same_client_on_different_threads()
        {
            RedisConfig.AssertAccessOnlyOnSameThread = true;
            InvalidAccessException poolEx = null;

            var redisManager = new RedisManagerPool();

            using (var redis = redisManager.GetClient())
            {
                var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                var key = $"Thread#{threadId}";
                redis.SetValue(key, threadId);
    
                ThreadPool.QueueUserWorkItem(_ => 
                {
                    using (var poolRedis = redisManager.GetClient())
                    {
                        var poolThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                        var poolKey = $"Thread#{poolThreadId}";
                        poolRedis.SetValue(poolKey , poolThreadId);
                            
                        Console.WriteLine("From Pool: " + poolRedis.GetValue(poolKey));

                        try
                        {
                            Console.WriteLine("From Pool (using TEST): " + redis.GetValue(poolKey));
                        }
                        catch (InvalidAccessException ex)
                        {
                            poolEx = ex;
                        }
                    }
                });
                
                Thread.Sleep(100);
                
                Console.WriteLine("From Test: " + redis.GetValue(key));
                
                if (poolEx == null)
                    throw new Exception("Should throw InvalidAccessException");
                
                Console.WriteLine("InvalidAccessException: " + poolEx.Message);
            }
            
            RedisConfig.AssertAccessOnlyOnSameThread = false;
        }

        [Test]
        public void Does_not_throw_when_using_different_clients_on_same_Thread()
        {
            RedisConfig.AssertAccessOnlyOnSameThread = true;
            InvalidAccessException poolEx = null;

            var redisManager = new RedisManagerPool();

            using (var redis = redisManager.GetClient())
            {
                var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                var key = $"Thread#{threadId}";
                redis.SetValue(key, threadId);                
    
                ThreadPool.QueueUserWorkItem(_ => 
                {
                    try
                    {
                        using (var poolRedis = redisManager.GetClient())
                        {
                            var poolThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                            var poolKey = $"Thread#{poolThreadId}";
                            poolRedis.SetValue(poolKey , poolThreadId);
                                
                            Console.WriteLine("From Pool: " + poolRedis.GetValue(poolKey ));
                        }
                    }
                    catch (InvalidAccessException ex)
                    {
                        poolEx = ex;
                    }
                });
                
                Thread.Sleep(100);
                
                Console.WriteLine("From Test: " + redis.GetValue(key));            
            }
            
            RedisConfig.AssertAccessOnlyOnSameThread = false;
        }
    }
}