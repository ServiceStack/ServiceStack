using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace ServiceStack.Redis.Tests.Integration
{
    [Category("Integration")]
    public class IntegrationTestBase
    {
        protected IRedisClientsManager CreateAndStartPoolManager(
            string[] readWriteHosts, string[] readOnlyHosts)
        {
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts);
        }

        protected IRedisClientsManager CreateAndStartManagerPool(
            string[] readWriteHosts, string[] readOnlyHosts)
        {
            return new RedisManagerPool(readWriteHosts, new RedisPoolConfig
            {
                MaxPoolSize = 10
            });
        }

        protected IRedisClientsManager CreateAndStartBasicCacheManager(
            string[] readWriteHosts, string[] readOnlyHosts)
        {
            return new BasicRedisClientManager(readWriteHosts, readOnlyHosts);
        }

        protected IRedisClientsManager CreateAndStartBasicManager(
            string[] readWriteHosts, string[] readOnlyHosts)
        {
            return new BasicRedisClientManager(readWriteHosts, readOnlyHosts);
        }

        [Conditional("DEBUG")]
        protected static void Log(string fmt, params object[] args)
        {
            Debug.WriteLine(String.Format(fmt, args));
        }

        protected void RunSimultaneously(
            Func<string[], string[], IRedisClientsManager> clientManagerFactory,
            Action<IRedisClientsManager, int> useClientFn)
        {
            var before = Stopwatch.GetTimestamp();

            const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

#if NETCORE
            List<Task> tasks = new List<Task>();
#else
            var clientAsyncResults = new List<IAsyncResult>();
#endif             
            using (var manager = clientManagerFactory(TestConfig.MasterHosts, TestConfig.ReplicaHosts))
            {
                for (var i = 0; i < noOfConcurrentClients; i++)
                {
                    var clientNo = i;
                    var action = (Action)(() => useClientFn(manager, clientNo));
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

            Debug.WriteLine($"Time Taken: {(Stopwatch.GetTimestamp() - before) / 1000}");
        }

        protected static void CheckHostCountMap(Dictionary<string, int> hostCountMap)
        {
            Debug.WriteLine(TypeSerializer.SerializeToString(hostCountMap));

            if (TestConfig.ReplicaHosts.Length <= 1) return;

            var hostCount = 0;
            foreach (var entry in hostCountMap)
            {
                if (entry.Value < 5)
                {
                    Debug.WriteLine("ERROR: Host has unproportionate distribution: " + entry.Value);
                }
                if (entry.Value > 60)
                {
                    Debug.WriteLine("ERROR: Host has unproportionate distribution: " + entry.Value);
                }
                hostCount += entry.Value;
            }

            if (hostCount != 64)
            {
                Debug.WriteLine("ERROR: Invalid no of clients used");
            }
        }

    }
}