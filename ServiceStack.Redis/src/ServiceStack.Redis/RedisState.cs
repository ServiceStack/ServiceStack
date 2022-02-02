using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Don't immediately kill connections of active clients after failover to give them a chance to dispose gracefully. 
    /// Deactivating clients are automatically cleared from the pool.
    /// </summary>
    internal static class RedisState
    {
        private static ILog log = LogManager.GetLogger(typeof(RedisState));

        internal static long TotalCommandsSent = 0;
        internal static long TotalFailovers = 0;
        internal static long TotalDeactivatedClients = 0;
        internal static long TotalFailedSentinelWorkers = 0;
        internal static long TotalForcedMasterFailovers = 0;
        internal static long TotalInvalidMasters = 0;
        internal static long TotalNoMastersFound = 0;
        internal static long TotalClientsCreated = 0;
        internal static long TotalClientsCreatedOutsidePool = 0;
        internal static long TotalSubjectiveServersDown = 0;
        internal static long TotalObjectiveServersDown = 0;
        internal static long TotalRetryCount = 0;
        internal static long TotalRetrySuccess = 0;
        internal static long TotalRetryTimedout = 0;

        internal static readonly ConcurrentDictionary<RedisClient, DateTime> DeactivatedClients = new ConcurrentDictionary<RedisClient, DateTime>();

        internal static void DeactivateClient(RedisClient client)
        {
            Interlocked.Increment(ref TotalDeactivatedClients);

            if (RedisConfig.DeactivatedClientsExpiry == TimeSpan.Zero)
            {
                client.DisposeConnection();
                return;
            }

            var deactivatedAt = client.DeactivatedAt ?? DateTime.UtcNow;
            client.DeactivatedAt = deactivatedAt;

            if (!DeactivatedClients.TryAdd(client, deactivatedAt))
                client.DisposeConnection();
        }

        internal static void DisposeExpiredClients()
        {
            if (RedisConfig.DeactivatedClientsExpiry == TimeSpan.Zero || DeactivatedClients.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var removeDisposed = new List<RedisClient>();

            foreach (var entry in DeactivatedClients)
            {
                try
                {
                    if (now - entry.Value <= RedisConfig.DeactivatedClientsExpiry)
                        continue;

                    if (log.IsDebugEnabled)
                        log.Debug("Disposed Deactivated Client: {0}".Fmt(entry.Key.GetHostString()));

                    entry.Key.DisposeConnection();
                    removeDisposed.Add(entry.Key);
                }
                catch
                {
                    removeDisposed.Add(entry.Key);
                }
            }

            if (removeDisposed.Count == 0)
                return;

            var dict = ((IDictionary<RedisClient, DateTime>)DeactivatedClients);
            foreach (var client in removeDisposed)
            {
                dict.Remove(client);
            }
        }

        internal static void DisposeAllDeactivatedClients()
        {
            if (RedisConfig.DeactivatedClientsExpiry == TimeSpan.Zero)
                return;

            var allClients = DeactivatedClients.Keys.ToArray();
            DeactivatedClients.Clear();
            foreach (var client in allClients)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Disposed Deactivated Client (All): {0}".Fmt(client.GetHostString()));

                client.DisposeConnection();
            }
        }
    }
}