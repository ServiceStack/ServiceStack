using System.Collections.Generic;
using System.Threading;

namespace ServiceStack.Redis
{
    public static class RedisStats
    {
        /// <summary>
        /// Total number of commands sent
        /// </summary>
        public static long TotalCommandsSent
        {
            get { return Interlocked.Read(ref RedisState.TotalCommandsSent); }
        }

        /// <summary>
        /// Number of times the Redis Client Managers have FailoverTo() either by sentinel or manually
        /// </summary>
        public static long TotalFailovers
        {
            get { return Interlocked.Read(ref RedisState.TotalFailovers); }
        }

        /// <summary>
        /// Number of times a Client was deactivated from the pool, either by FailoverTo() or exceptions on client
        /// </summary>
        public static long TotalDeactivatedClients
        {
            get { return Interlocked.Read(ref RedisState.TotalDeactivatedClients); }
        }

        /// <summary>
        /// Number of times connecting to a Sentinel has failed
        /// </summary>
        public static long TotalFailedSentinelWorkers
        {
            get { return Interlocked.Read(ref RedisState.TotalFailedSentinelWorkers); }
        }

        /// <summary>
        /// Number of times we've forced Sentinel to failover to another master due to 
        /// consecutive errors beyond sentinel.WaitBeforeForcingMasterFailover
        /// </summary>
        public static long TotalForcedMasterFailovers
        {
            get { return Interlocked.Read(ref RedisState.TotalForcedMasterFailovers); }
        }

        /// <summary>
        /// Number of times a connecting to a reported Master wasn't actually a Master
        /// </summary>
        public static long TotalInvalidMasters
        {
            get { return Interlocked.Read(ref RedisState.TotalInvalidMasters); }
        }

        /// <summary>
        /// Number of times no Masters could be found in any of the configured hosts
        /// </summary>
        public static long TotalNoMastersFound
        {
            get { return Interlocked.Read(ref RedisState.TotalNoMastersFound); }
        }

        /// <summary>
        /// Number of Redis Client instances created with RedisConfig.ClientFactory
        /// </summary>
        public static long TotalClientsCreated
        {
            get { return Interlocked.Read(ref RedisState.TotalClientsCreated); }
        }

        /// <summary>
        /// Number of times a Redis Client was created outside of pool, either due to overflow or reserved slot was overridden
        /// </summary>
        public static long TotalClientsCreatedOutsidePool
        {
            get { return Interlocked.Read(ref RedisState.TotalClientsCreatedOutsidePool); }
        }

        /// <summary>
        /// Number of times Redis Sentinel reported a Subjective Down (sdown)
        /// </summary>
        public static long TotalSubjectiveServersDown
        {
            get { return Interlocked.Read(ref RedisState.TotalSubjectiveServersDown); }
        }

        /// <summary>
        /// Number of times Redis Sentinel reported an Objective Down (sdown)
        /// </summary>
        public static long TotalObjectiveServersDown
        {
            get { return Interlocked.Read(ref RedisState.TotalObjectiveServersDown); }
        }

        /// <summary>
        /// Number of times a Redis Request was retried due to Socket or Retryable exception
        /// </summary>
        public static long TotalRetryCount
        {
            get { return Interlocked.Read(ref RedisState.TotalRetryCount); }
        }

        /// <summary>
        /// Number of times a Request succeeded after it was retried
        /// </summary>
        public static long TotalRetrySuccess
        {
            get { return Interlocked.Read(ref RedisState.TotalRetrySuccess); }
        }

        /// <summary>
        /// Number of times a Retry Request failed after exceeding RetryTimeout
        /// </summary>
        public static long TotalRetryTimedout
        {
            get { return Interlocked.Read(ref RedisState.TotalRetryTimedout); }
        }

        /// <summary>
        /// Total number of deactivated clients that are pending being disposed
        /// </summary>
        public static long TotalPendingDeactivatedClients
        {
            get { return RedisState.DeactivatedClients.Count; }
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref RedisState.TotalFailovers, 0);
            Interlocked.Exchange(ref RedisState.TotalDeactivatedClients, 0);
            Interlocked.Exchange(ref RedisState.TotalFailedSentinelWorkers, 0);
            Interlocked.Exchange(ref RedisState.TotalForcedMasterFailovers, 0);
            Interlocked.Exchange(ref RedisState.TotalInvalidMasters, 0);
            Interlocked.Exchange(ref RedisState.TotalNoMastersFound, 0);
            Interlocked.Exchange(ref RedisState.TotalClientsCreated, 0);
            Interlocked.Exchange(ref RedisState.TotalClientsCreatedOutsidePool, 0);
            Interlocked.Exchange(ref RedisState.TotalSubjectiveServersDown, 0);
            Interlocked.Exchange(ref RedisState.TotalObjectiveServersDown, 0);
            Interlocked.Exchange(ref RedisState.TotalRetryCount, 0);
            Interlocked.Exchange(ref RedisState.TotalRetrySuccess, 0);
            Interlocked.Exchange(ref RedisState.TotalRetryTimedout, 0);
        }

        public static Dictionary<string, long> ToDictionary()
        {
            return new Dictionary<string, long>
            {
                {"TotalCommandsSent", TotalCommandsSent},
                {"TotalFailovers", TotalFailovers},
                {"TotalDeactivatedClients", TotalDeactivatedClients},
                {"TotalFailedSentinelWorkers", TotalFailedSentinelWorkers},
                {"TotalForcedMasterFailovers", TotalForcedMasterFailovers},
                {"TotalInvalidMasters", TotalInvalidMasters},
                {"TotalNoMastersFound", TotalNoMastersFound},
                {"TotalClientsCreated", TotalClientsCreated},
                {"TotalClientsCreatedOutsidePool", TotalClientsCreatedOutsidePool},
                {"TotalSubjectiveServersDown", TotalSubjectiveServersDown},
                {"TotalObjectiveServersDown", TotalObjectiveServersDown},
                {"TotalPendingDeactivatedClients", TotalPendingDeactivatedClients },
                {"TotalRetryCount", TotalRetryCount },
                {"TotalRetrySuccess", TotalRetrySuccess },
                {"TotalRetryTimedout", TotalRetryTimedout },
            };
        }
    }
}