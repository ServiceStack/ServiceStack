#if !(NETSTANDARD2_0 || NETSTANDARD2_1 || NET6_0)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ServiceStack.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Support.Diagnostic
{
    /// <summary>
    /// Tracks each IRedisClient instance allocated from the IRedisClientsManager logging when they are allocated and disposed. 
    /// Periodically writes the allocated instances to the log for diagnostic purposes.
    /// </summary>
    public partial class TrackingRedisClientsManager : IRedisClientsManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrackingRedisClientsManager));

        private readonly HashSet<TrackingFrame> trackingFrames = new HashSet<TrackingFrame>();
        private readonly IRedisClientsManager redisClientsManager;

        public TrackingRedisClientsManager(IRedisClientsManager redisClientsManager)
        {
            this.redisClientsManager = redisClientsManager ?? throw new ArgumentNullException(nameof(redisClientsManager));
            Logger.DebugFormat("Constructed");

            var timer = new Timer(state => this.DumpState());
            timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        }

        public void Dispose()
        {
            Logger.DebugFormat("Disposed");
            this.redisClientsManager.Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IRedisClient GetClient()
        {
            // get calling instance
            var callingStackFrame = new StackFrame(1, true);
            var callingMethodType = callingStackFrame.GetMethod();

            return TrackInstance(callingMethodType, "GetClient", this.redisClientsManager.GetClient());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IRedisClient GetReadOnlyClient()
        {
            // get calling instance
            var callingMethodType = new StackFrame(1, true).GetMethod();

            return TrackInstance(callingMethodType, "GetReadOnlyClient", this.redisClientsManager.GetReadOnlyClient());
        }

        public ICacheClient GetCacheClient()
        {
            Logger.DebugFormat("GetCacheClient");
            return this.redisClientsManager.GetCacheClient();
        }

        public ICacheClient GetReadOnlyCacheClient()
        {
            Logger.DebugFormat("GetReadOnlyCacheClient");
            return this.redisClientsManager.GetReadOnlyCacheClient();
        }

        private IRedisClient TrackInstance(MethodBase callingMethodType, string method, IRedisClient instance)
        {
            // track
            var frame = new TrackingFrame()
            {
                Id = Guid.NewGuid(),
                Initialised = DateTime.Now,
                ProvidedToInstanceOfType = callingMethodType.DeclaringType,
            };
            lock (this.trackingFrames)
            {
                this.trackingFrames.Add(frame);
            }

            // proxy
            var proxy = new TrackingRedisClientProxy(instance, frame.Id);
            proxy.BeforeInvoke += (sender, args) =>
            {
                if (string.Compare("Dispose", args.MethodInfo.Name, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }
                lock (this.trackingFrames)
                {
                    this.trackingFrames.Remove(frame);
                }
                var duration = DateTime.Now - frame.Initialised;

                Logger.DebugFormat("{0,18} Disposed {1} released from instance of type {2} checked out for {3}", method, frame.Id, frame.ProvidedToInstanceOfType.FullName, duration);
            };

            Logger.DebugFormat("{0,18} Tracking {1} allocated to instance of type {2}", method, frame.Id, frame.ProvidedToInstanceOfType.FullName);
            return proxy.GetTransparentProxy() as IRedisClient;
        }

        private void DumpState()
        {
            Logger.InfoFormat("Dumping currently checked out IRedisClient instances");
            var inUseInstances = new Func<TrackingFrame[]>(() =>
            {
                lock (this.trackingFrames)
                {
                    return Enumerable.ToArray(this.trackingFrames);
                }
            }).Invoke();

            var summaryByType = inUseInstances.GroupBy(x => x.ProvidedToInstanceOfType.FullName);
            foreach (var grouped in summaryByType)
            {
                Logger.InfoFormat("{0,60}: {1,-9} oldest {2}", grouped.Key, grouped.Count(),
                    grouped.Min(x => x.Initialised));
            }

            foreach (var trackingFrame in inUseInstances)
            {
                Logger.DebugFormat("Instance {0} allocated to {1} at {2} ({3})", trackingFrame.Id,
                    trackingFrame.ProvidedToInstanceOfType.FullName, trackingFrame.Initialised,
                    trackingFrame.ProvidedToInstanceOfType.FullName);
            }
        }
    }
}
#endif