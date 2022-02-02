using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace ServiceStack.Redis.Tests.Integration
{
    [TestFixture]
    public class MultiThreadedRedisClientIntegrationTests
        : IntegrationTestBase
    {
        private static string testData;

        [OneTimeSetUp]
        public void onBeforeTestFixture()
        {
            var results = 100.Times(x => ModelWithFieldsOfDifferentTypes.Create(x));

            testData = TypeSerializer.SerializeToString(results);
        }

        [Test]
        public void Can_support_64_threads_using_the_client_simultaneously()
        {
            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

#if NETCORE
            List<Task> tasks = new List<Task>();
#else
            var clientAsyncResults = new List<IAsyncResult>();
#endif             
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    var action = (Action)(() => UseClientAsync(redisClient, clientNo));
#if NETCORE
                    tasks.Add(Task.Run(action));
#else                                       
                    clientAsyncResults.Add(action.BeginInvoke(null, null));
#endif
                }
            }
#if NETCORE
            Task.WaitAll(tasks.ToArray());
#else            
            WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());
#endif
            Debug.WriteLine(String.Format("Time Taken: {0}", (Stopwatch.GetTimestamp() - before) / 1000));
        }

        [Test]
        public void Can_support_64_threads_using_the_client_sequentially()
        {
            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    UseClient(redisClient, clientNo);
                }
            }

            Debug.WriteLine(String.Format("Time Taken: {0}", (Stopwatch.GetTimestamp() - before) / 1000));
        }

        private void UseClientAsync(RedisClient client, int clientNo)
        {
            lock (this)
            {
                UseClient(client, clientNo);
            }
        }

        private static void UseClient(RedisClient client, int clientNo)
        {
            var host = "";

            try
            {
                host = client.Host;

                Log("Client '{0}' is using '{1}'", clientNo, client.Host);

                var testClientKey = "test:" + host + ":" + clientNo;
                client.SetValue(testClientKey, testData);
                var result = client.GetValue(testClientKey) ?? "";

                Log("\t{0} => {1} len {2} {3} len", testClientKey,
                    testData.Length, testData.Length == result.Length ? "==" : "!=", result.Length);

            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("NullReferenceException StackTrace: \n" + ex.StackTrace);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("\t[ERROR@{0}]: {1} => {2}",
                    host, ex.GetType().Name, ex.Message));
            }
        }

    }
}