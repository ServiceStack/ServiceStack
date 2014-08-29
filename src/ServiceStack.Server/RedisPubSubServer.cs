using System;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack
{
    public interface IRedisPubSubServer : IDisposable
    {
        Action OnInit { get; set; }
        Action OnStart { get; set; }
        Action OnStop { get; set; }
        Action OnDispose { get; set; }
        Action<string, string> OnMessage { get; set; }
        Action<string> OnUnSubscribe { get; set; }
        Action<Exception> OnError { get; set; }
        Action<IRedisPubSubServer> OnFailover { get; set; }

        IRedisClientsManager ClientsManager { get; }
        string[] Channels { get; }

        int? KeepAliveRetryAfterMs { get; set; }
        DateTime CurrentServerTime { get; }

        void Start();
        void Stop();
        void Restart();
    }

    public class RedisPubSubServer : IRedisPubSubServer
    {
        private static ILog Log = LogManager.GetLogger(typeof(RedisPubSubServer));
        private DateTime serverTimeAtStart;
        private Stopwatch startedAt;

        public Action OnInit { get; set; }
        public Action OnStart { get; set; }
        public Action OnStop { get; set; }
        public Action OnDispose { get; set; }
        public Action<string, string> OnMessage { get; set; }
        public Action<string> OnUnSubscribe { get; set; }
        public Action<Exception> OnError { get; set; }
        public Action<IRedisPubSubServer> OnFailover { get; set; }

        readonly Random rand = new Random(Environment.TickCount);
        public int? KeepAliveRetryAfterMs { get; set; }

        private int doOperation = WorkerOperation.NoOp;

        private long timesStarted = 0;
        private long noOfErrors = 0;
        private int noOfContinuousErrors = 0;
        private string lastExMsg = null;
        private int status;
        private Thread bgThread; //Subscription controller thread
        private long bgThreadCount = 0;

        public DateTime CurrentServerTime
        {
            get { return new DateTime(serverTimeAtStart.Ticks + startedAt.ElapsedTicks, DateTimeKind.Utc); }
        }

        public long BgThreadCount
        {
            get { return Interlocked.CompareExchange(ref bgThreadCount, 0, 0); }
        }

        public IRedisClientsManager ClientsManager { get; set; }
        public string[] Channels { get; set; }

        public RedisPubSubServer(IRedisClientsManager clientsManager, params string[] channels)
        {
            this.ClientsManager = clientsManager;
            this.Channels = channels;

            var failoverHost = clientsManager as IRedisFailover;
            if (failoverHost != null)
            {
                failoverHost.OnFailover.Add(HandleFailover);
            }
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
            {
                //Start any stopped worker threads
                if (OnStart != null)
                    OnStart();

                return;
            }
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("RedisPubSubServer has been disposed");

            //Only 1 thread allowed past
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped) //Should only be 1 thread past this point
            {
                try
                {
                    Init();

                    SleepBackOffMultiplier(Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));

                    if (OnStart != null)
                        OnStart();

                    //Don't kill us if we're the thread that's retrying to Start() after a failure.
                    if (bgThread != Thread.CurrentThread)
                    {
                        KillBgThreadIfExists();

                        bgThread = new Thread(RunLoop)
                        {
                            IsBackground = true,
                            Name = "RedisPubSubServer " + Interlocked.Increment(ref bgThreadCount)
                        };
                        bgThread.Start();
                        Log.Debug("Started Background Thread: " + bgThread.Name);
                    }
                    else
                    {
                        Log.Debug("Retrying RunLoop() on Thread: " + bgThread.Name);
                        RunLoop();
                    }
                }
                catch (Exception ex)
                {
                    ex.Message.Print();
                    if (this.OnError != null) this.OnError(ex);
                }
            }
        }

        private void Init()
        {
            using (var redis = ClientsManager.GetReadOnlyClient())
            {
                serverTimeAtStart = redis.GetServerTime();
                startedAt = Stopwatch.StartNew();
            }

            if (OnInit != null)
                OnInit();
        }

        private IRedisClient masterClient;
        private void RunLoop()
        {
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting) return;
            Interlocked.Increment(ref timesStarted);

            try
            {
                //RESET
                while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                {
                    using (var redis = ClientsManager.GetReadOnlyClient())
                    {
                        masterClient = redis;

                        //Record that we had a good run...
                        Interlocked.CompareExchange(ref noOfContinuousErrors, 0, noOfContinuousErrors);

                        using (var subscription = redis.CreateSubscription())
                        {
                            subscription.OnUnSubscribe = HandleUnSubscribe;

                            subscription.OnMessage = (channel, msg) =>
                            {
                                if (msg == WorkerOperation.ControlCommand)
                                {
                                    var op = Interlocked.CompareExchange(ref doOperation, WorkerOperation.NoOp, doOperation);
                                    switch (op)
                                    {
                                        case WorkerOperation.Stop:
                                            Log.Debug("Stop Command Issued");

                                            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                                Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                                            Log.Debug("UnSubscribe From All Channels...");
                                            subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                            return;

                                        case WorkerOperation.Reset:
                                            subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                            return;
                                    }
                                }

                                if (!string.IsNullOrEmpty(msg))
                                {
                                    OnMessage(channel, msg);
                                }
                            };

                            subscription.SubscribeToChannels(Channels); //blocks thread
                            masterClient = null;
                        }
                    }
                }

                if (OnStop != null)
                    OnStop();
            }
            catch (Exception ex)
            {
                lastExMsg = ex.Message;
                Interlocked.Increment(ref noOfErrors);
                Interlocked.Increment(ref noOfContinuousErrors);

                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                    Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                if (OnStop != null)
                    OnStop();

                if (this.OnError != null)
                    this.OnError(ex);

                if (KeepAliveRetryAfterMs != null)
                {
                    Thread.Sleep(KeepAliveRetryAfterMs.Value);
                    Start();
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("RedisPubSubServer has been disposed");

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                Log.Debug("Stopping RedisPubSubServer...");

                //Unblock current bgthread by issuing StopCommand
                try
                {
                    using (var redis = ClientsManager.GetClient())
                    {
                        Interlocked.CompareExchange(ref doOperation, WorkerOperation.Stop, doOperation);
                        Channels.Each(x => 
                            redis.PublishMessage(x, WorkerOperation.ControlCommand));
                    }
                }
                catch (Exception ex)
                {
                    if (this.OnError != null) this.OnError(ex);
                    Log.Warn("Could not send STOP message to bg thread: " + ex.Message);
                }
            }
        }

        private void HandleFailover(IRedisClientsManager clientsManager)
        {
            try
            {
                if (OnFailover != null)
                    OnFailover(this);

                if (masterClient != null)
                {
                    //New thread-safe client with same connection info as connected master
                    using (var currentlySubscribedClient = ((RedisClient)masterClient).CloneClient())
                    {
                        Interlocked.CompareExchange(ref doOperation, WorkerOperation.Reset, doOperation);
                        Channels.Each(x => 
                            currentlySubscribedClient.PublishMessage(x, WorkerOperation.ControlCommand));
                    }
                }
                else
                {
                    Restart();
                }
            }
            catch (Exception ex)
            {
                if (this.OnError != null) this.OnError(ex);
                Log.Warn("Error trying to UnSubscribeFromChannels in OnFailover. Restarting...", ex);
                Restart();
            }
        }

        void HandleUnSubscribe(string channel)
        {
            Log.Debug("OnUnSubscribe: " + channel);

            if (OnUnSubscribe != null)
                OnUnSubscribe(channel);
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void KillBgThreadIfExists()
        {
            if (bgThread != null && bgThread.IsAlive)
            {
                //give it a small chance to die gracefully
                if (!bgThread.Join(500))
                {
                    //Ideally we shouldn't get here, but lets try our hardest to clean it up
                    Log.Warn("Interrupting previous Background Thread: " + bgThread.Name);
                    bgThread.Interrupt();
                    if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                    {
                        Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
                        bgThread.Abort();
                    }
                }
                bgThread = null;
            }
        }

        private void SleepBackOffMultiplier(int continuousErrorsCount)
        {
            if (continuousErrorsCount == 0) return;
            const int MaxSleepMs = 60 * 1000;

            //exponential/random retry back-off.
            var nextTry = Math.Min(
                rand.Next((int)Math.Pow(continuousErrorsCount, 3), (int)Math.Pow(continuousErrorsCount + 1, 3) + 1),
                MaxSleepMs);

            Log.Debug("Sleeping for {0}ms after {1} continuous errors".Fmt(nextTry, continuousErrorsCount));

            Thread.Sleep(nextTry);
        }

        public virtual void Dispose()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            Stop();

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopped) != WorkerStatus.Stopped)
                Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopping);

            try
            {
                if (OnDispose != null)
                    OnDispose();
            }
            catch (Exception ex)
            {
                Log.Error("Error OnDispose(): ", ex);
            }

            try
            {
                Thread.Sleep(100); //give it a small chance to die gracefully
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                if (this.OnError != null) this.OnError(ex);
            }
        } 
    }
}