using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
    [TestFixture, Category("Integration")]
    public class RedisRegressionTestRun
    {
        private static string testData;

        [OneTimeSetUp]
        public void onBeforeTestFixture()
        {
            var results = 100.Times(x => ModelWithFieldsOfDifferentTypes.Create(x));

            testData = TypeSerializer.SerializeToString(results);
        }

        [Ignore("Can hang CI")]
        [Test]
        public void Can_support_64_threads_using_the_client_simultaneously()
        {
            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

            var clientAsyncResults = new List<IAsyncResult>();
            using (var manager = new PooledRedisClientManager(TestConfig.MasterHosts, TestConfig.ReplicaHosts))
            {
                manager.GetClient().Run(x => x.FlushAll());

                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    var action = (Action)(() => UseClientAsync(manager, clientNo));
                    clientAsyncResults.Add(action.BeginInvoke(null, null));
                }
            }

            WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

            Debug.WriteLine(string.Format("Completed in {0} ticks", (Stopwatch.GetTimestamp() - before)));

            RedisStats.ToDictionary().PrintDump();
        }

        [Test]
        public void Can_run_series_of_operations_sequentially()
        {
            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                redisClient.FlushAll();

                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    UseClient(redisClient, clientNo);
                }
            }

            Debug.WriteLine(String.Format("Completed in {0} ticks", (Stopwatch.GetTimestamp() - before)));
        }

        private static void UseClientAsync(IRedisClientsManager manager, int clientNo)
        {
            using (var client = manager.GetClient())
            {
                UseClient(client, clientNo);
            }
        }

        private static void UseClient(IRedisClient client, int clientNo)
        {
            var host = "";

            try
            {
                host = client.Host;

                Debug.WriteLine(string.Format("Client '{0}' is using '{1}'", clientNo, client.Host));
                var differentDbs = new[] { 1, 0, 2 };

                foreach (var db in differentDbs)
                {
                    client.Db = db;

                    var testClientKey = "test:" + host + ":" + clientNo;
                    client.SetValue(testClientKey, testData);
                    var result = client.GetValue(testClientKey) ?? "";
                    LogResult(db, testClientKey, result);

                    var testClientSetKey = "test+set:" + host + ":" + clientNo;
                    client.AddItemToSet(testClientSetKey, testData);
                    var resultSet = client.GetAllItemsFromSet(testClientSetKey);
                    LogResult(db, testClientKey, resultSet.ToList().FirstOrDefault());

                    var testClientListKey = "test+list:" + host + ":" + clientNo;
                    client.AddItemToList(testClientListKey, testData);
                    var resultList = client.GetAllItemsFromList(testClientListKey);
                    LogResult(db, testClientKey, resultList.FirstOrDefault());
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("NullReferenceException StackTrace: \n" + ex.StackTrace);
                Assert.Fail("NullReferenceException");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("\t[ERROR@{0}]: {1} => {2}",
                    host, ex.GetType().Name, ex));
                Assert.Fail("Exception");
            }
        }

        private static void LogResult(int db, string testClientKey, string resultData)
        {
            if (resultData.IsNullOrEmpty())
            {
                Debug.WriteLine(String.Format("\tERROR@[{0}] NULL", db));
                return;
            }

            Debug.WriteLine(String.Format("\t[{0}] {1} => {2} len {3} {4} len",
              db,
              testClientKey,
              testData.Length,
              testData.Length == resultData.Length ? "==" : "!=", resultData.Length));
        }
    }

}