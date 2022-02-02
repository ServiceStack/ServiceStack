using System;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public class RedisPubSubServer : IRedisPubSubServer
    {
        private static ILog Log = LogManager.GetLogger(typeof(RedisPubSubServer));
        private DateTime serverTimeAtStart;
        private Stopwatch startedAt;

        public TimeSpan? HeartbeatInterval = TimeSpan.FromSeconds(10);
        public TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);
        private long lastHeartbeatTicks;
        private Timer heartbeatTimer;

        public Action OnInit { get; set; }
        public Action OnStart { get; set; }
        public Action OnHeartbeatSent { get; set; }
        public Action OnHeartbeatReceived { get; set; }
        public Action OnStop { get; set; }
        public Action OnDispose { get; set; }
        
        /// <summary>
        /// Callback fired on each message received, handle with (channel, msg) => ... 
        /// </summary>
        public Action<string, string> OnMessage { get; set; }
        public Action<string, byte[]> OnMessageBytes { get; set; }

        public Action<string> OnControlCommand { get; set; }
        public Action<string> OnUnSubscribe { get; set; }
        public Action<string> OnEvent { get; set; }
        public Action<Exception> OnError { get; set; }
        public Action<IRedisPubSubServer> OnFailover { get; set; }
        public bool IsSentinelSubscription { get; set; }

        readonly Random rand = new Random(Environment.TickCount);

        private int doOperation = Operation.NoOp;

        private long timesStarted = 0;
        private long noOfErrors = 0;
        private int noOfContinuousErrors = 0;
        private string lastExMsg = null;
        private int status;
        private Thread bgThread; //Subscription controller thread
        private long bgThreadCount = 0;

        private const int NO = 0;
        private const int YES = 1;

        private int autoRestart = YES;
        public bool AutoRestart
        {
            get => Interlocked.CompareExchange(ref autoRestart, 0, 0) == YES;
            set => Interlocked.CompareExchange(ref autoRestart, value ? YES : NO, autoRestart);
        }

        public DateTime CurrentServerTime => new DateTime(serverTimeAtStart.Ticks + startedAt.Elapsed.Ticks, DateTimeKind.Utc);

        public long BgThreadCount => Interlocked.CompareExchange(ref bgThreadCount, 0, 0);

        public const string AllChannelsWildCard = "*";
        public IRedisClientsManager ClientsManager { get; set; }
        public string[] Channels { get; set; }
        public string[] ChannelsMatching { get; set; }
        public TimeSpan? WaitBeforeNextRestart { get; set; }

        public RedisPubSubServer(IRedisClientsManager clientsManager, params string[] channels)
        {
            this.ClientsManager = clientsManager;
            this.Channels = channels;
            startedAt = Stopwatch.StartNew();

            var failoverHost = clientsManager as IRedisFailover;
            failoverHost?.OnFailover.Add(HandleFailover);
        }

        public IRedisPubSubServer Start()
        {
            AutoRestart = true;

            if (Interlocked.CompareExchange(ref status, 0, 0) == Status.Started)
            {
                //Start any stopped worker threads
                OnStart?.Invoke();

                return this;
            }
            if (Interlocked.CompareExchange(ref status, 0, 0) == Status.Disposed)
                throw new ObjectDisposedException("RedisPubSubServer has been disposed");

            //Only 1 thread allowed past
            if (Interlocked.CompareExchange(ref status, Status.Starting, Status.Stopped) == Status.Stopped) //Should only be 1 thread past this point
            {
                OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} Stopped] Start()> Stopped -> Starting");

                var initErrors = 0;
                bool hasInit = false;
                while (!hasInit)
                {
                    try
                    {
                        Init();
                        hasInit = true;
                    }
                    catch (Exception ex)
                    {
                        OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] Start().Init()> Exception: {ex.Message}");
                        OnError?.Invoke(ex);
                        SleepBackOffMultiplier(initErrors++);
                    }
                }

                try
                {
                    SleepBackOffMultiplier(Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));

                    OnStart?.Invoke();

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
                        if (Log.IsDebugEnabled)
                            Log.Debug("Started Background Thread: " + bgThread.Name);
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                            Log.Debug("Retrying RunLoop() on Thread: " + bgThread.Name);
                        RunLoop();
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
            }

            return this;
        }

        private void Init()
        {
            using (var redis = ClientsManager.GetReadOnlyClient())
            {
                startedAt = Stopwatch.StartNew();
                serverTimeAtStart = IsSentinelSubscription
                    ? DateTime.UtcNow
                    : redis.GetServerTime();
            }

            DisposeHeartbeatTimer();

            if (HeartbeatInterval != null)
            {
                heartbeatTimer = new Timer(SendHeartbeat, null, 
                    TimeSpan.FromMilliseconds(0), HeartbeatInterval.GetValueOrDefault());
            }

            Interlocked.CompareExchange(ref lastHeartbeatTicks, DateTime.UtcNow.Ticks, lastHeartbeatTicks);

            OnInit?.Invoke();
        }

        void SendHeartbeat(object state)
        {
            var currentStatus = Interlocked.CompareExchange(ref status, 0, 0);
            if (currentStatus != Status.Started)
                return;

            if (DateTime.UtcNow - new DateTime(lastHeartbeatTicks) < HeartbeatInterval.GetValueOrDefault())
                return;

            OnHeartbeatSent?.Invoke();

            NotifyAllSubscribers(ControlCommand.Pulse);

            if (DateTime.UtcNow - new DateTime(lastHeartbeatTicks) > HeartbeatTimeout)
            {
                currentStatus = Interlocked.CompareExchange(ref status, 0, 0);

                OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {Status.GetStatus(currentStatus)}] SendHeartbeat()> Exceeded HeartbeatTimeout");
                if (currentStatus == Status.Started)
                {
                    Restart();
                }
            }
        }

        void Pulse()
        {
            Interlocked.CompareExchange(ref lastHeartbeatTicks, DateTime.UtcNow.Ticks, lastHeartbeatTicks);

            OnHeartbeatReceived?.Invoke();
        }

        private void DisposeHeartbeatTimer()
        {
            if (heartbeatTimer == null)
                return;

            try
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("RedisPubServer.DisposeHeartbeatTimer()");
                
                heartbeatTimer.Dispose();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
            heartbeatTimer = null;
        }

        private IRedisClient masterClient;
        private void RunLoop()
        {
            if (Interlocked.CompareExchange(ref status, Status.Started, Status.Starting) != Status.Starting) return;
            Interlocked.Increment(ref timesStarted);

            OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} Started] RunLoop().Stop> Starting -> Started, timesStarted: {timesStarted}");

            try
            {
                //RESET
                while (Interlocked.CompareExchange(ref status, 0, 0) == Status.Started)
                {
                    using var redis = ClientsManager.GetReadOnlyClient();
                    masterClient = redis;

                    //Record that we had a good run...
                    Interlocked.CompareExchange(ref noOfContinuousErrors, 0, noOfContinuousErrors);

                    using var subscription = redis.CreateSubscription();
                    subscription.OnUnSubscribe = HandleUnSubscribe;

                    if (OnMessageBytes != null)
                    {
                        bool IsCtrlMessage(byte[] msg)
                        {
                            if (msg.Length < 4)
                                return false;
                            return msg[0] == 'C' && msg[1] == 'T' && msg[0] == 'R' && msg[0] == 'L';
                        }
                                
                        ((RedisSubscription)subscription).OnMessageBytes = (channel, msg) => {
                            if (IsCtrlMessage(msg))
                                return;

                            OnMessageBytes(channel, msg);
                        };
                    }

                    subscription.OnMessage = (channel, msg) =>
                    {
                        if (string.IsNullOrEmpty(msg)) 
                            return;

                        var ctrlMsg = msg.LeftPart(':');
                        if (ctrlMsg == ControlCommand.Control)
                        {
                            var op = Interlocked.CompareExchange(ref doOperation, Operation.NoOp, doOperation);
                                    
                            var msgType = msg.IndexOf(':') >= 0
                                ? msg.RightPart(':')
                                : null;

                            OnControlCommand?.Invoke(msgType ?? Operation.GetName(op));

                            switch (op)
                            {
                                case Operation.Stop:
                                    if (Log.IsDebugEnabled)
                                        Log.Debug("Stop Command Issued");

                                    var holdStatus = GetStatus();
                                    
                                    Interlocked.CompareExchange(ref status, Status.Stopping, Status.Started);

                                    OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {holdStatus}] RunLoop().Stop> Started -> Stopping");
                                    try
                                    {
                                        if (Log.IsDebugEnabled)
                                            Log.Debug("UnSubscribe From All Channels...");

                                        OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] RunLoop().Stop> subscription.UnSubscribeFromAllChannels()");

                                        // ReSharper disable once AccessToDisposedClosure
                                        subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                    }
                                    finally
                                    {
                                        OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] RunLoop().Stop> Stopping -> Stopped");
                                        Interlocked.CompareExchange(ref status, Status.Stopped, Status.Stopping);
                                    }
                                    return;

                                case Operation.Reset:
                                    OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] RunLoop().Reset> subscription.UnSubscribeFromAllChannels()");

                                    // ReSharper disable once AccessToDisposedClosure
                                    subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                    return;
                            }

                            switch (msgType)
                            {
                                case ControlCommand.Pulse:
                                    Pulse();
                                    break;
                            }
                        }
                        else
                        {
                            OnMessage(channel, msg);
                        }
                    };

                    //blocks thread
                    if (ChannelsMatching != null && ChannelsMatching.Length > 0)
                        subscription.SubscribeToChannelsMatching(ChannelsMatching);
                    else
                        subscription.SubscribeToChannels(Channels);             

                    masterClient = null;
                }

                OnStop?.Invoke();
            }
            catch (Exception ex)
            {
                lastExMsg = ex.Message;
                Interlocked.Increment(ref noOfErrors);
                Interlocked.Increment(ref noOfContinuousErrors);
                
                var holdStatus = GetStatus();

                if (Interlocked.CompareExchange(ref status, Status.Stopped, Status.Started) != Status.Started)
                    Interlocked.CompareExchange(ref status, Status.Stopped, Status.Stopping);

                OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {holdStatus}] RunLoop().Stop> Started|Stopping -> Stopped");

                OnStop?.Invoke();

                OnError?.Invoke(ex);
            }

            if (AutoRestart && Interlocked.CompareExchange(ref status, 0, 0) != Status.Disposed)
            {
                if (WaitBeforeNextRestart != null)
                    TaskUtils.Sleep(WaitBeforeNextRestart.Value);

                OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] RunLoop().AutoRestart> Start()");
                Start();
            }
        }

        public void Stop()
        {
            Stop(shouldRestart:false);
        }

        private void Stop(bool shouldRestart)
        {
            AutoRestart = shouldRestart;

            if (Interlocked.CompareExchange(ref status, 0, 0) == Status.Disposed)
                throw new ObjectDisposedException("RedisPubSubServer has been disposed");

            if (Interlocked.CompareExchange(ref status, Status.Stopping, Status.Started) == Status.Started)
            {
                OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] Stop()> Started -> Stopping");

                if (Log.IsDebugEnabled)
                    Log.Debug("Stopping RedisPubSubServer...");

                //Unblock current bg thread by issuing StopCommand
                SendControlCommand(Operation.Stop);
            }
        }

        private void SendControlCommand(int operation)
        {
            Interlocked.CompareExchange(ref doOperation, operation, doOperation);
            NotifyAllSubscribers();
        }

        private void NotifyAllSubscribers(string commandType=null)
        {
            var msg = ControlCommand.Control;
            if (commandType != null)
                msg += ":" + commandType;

            try
            {
                using var redis = ClientsManager.GetClient();
                foreach (var channel in Channels)
                {
                    redis.PublishMessage(channel, msg);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Log.WarnFormat("Could not send '{0}' message to bg thread: {1}", msg, ex.Message);
            }
        }

        private void HandleFailover(IRedisClientsManager clientsManager)
        {
            try
            {
                OnFailover?.Invoke(this);

                if (masterClient != null)
                {
                    //New thread-safe client with same connection info as connected master
                    using var currentlySubscribedClient = ((RedisClient)masterClient).CloneClient();
                    Interlocked.CompareExchange(ref doOperation, Operation.Reset, doOperation);
                    foreach (var channel in Channels)
                    {
                        currentlySubscribedClient.PublishMessage(channel, ControlCommand.Control);
                    }
                }
                else
                {
                    Restart();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                Log.Warn("Error trying to UnSubscribeFromChannels in OnFailover. Restarting...", ex);
                Restart();
            }
        }

        void HandleUnSubscribe(string channel)
        {
            if (Log.IsDebugEnabled)
                Log.Debug("OnUnSubscribe: " + channel);

            OnUnSubscribe?.Invoke(channel);
        }

        public void Restart()
        {
            Stop(shouldRestart:true);
        }

        private void KillBgThreadIfExists()
        {
            if (bgThread != null && bgThread.IsAlive)
            {
                //give it a small chance to die gracefully
                if (!bgThread.Join(500))
                {
#if !NETCORE                    
                    //Ideally we shouldn't get here, but lets try our hardest to clean it up
                    OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] KillBgThreadIfExists()> bgThread.Interrupt()");
                    Log.Warn("Interrupting previous Background Thread: " + bgThread.Name);
                    bgThread.Interrupt();
                    if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                    {
                        OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] KillBgThreadIfExists()> bgThread.Abort()");
                        Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
                        bgThread.Abort();
                    }
#endif
                }
                bgThread = null;
            }
        }

        private void SleepBackOffMultiplier(int continuousErrorsCount)
        {
            if (continuousErrorsCount == 0) return;
            const int maxSleepMs = 60 * 1000;

            //exponential/random retry back-off.
            var nextTry = Math.Min(
                rand.Next((int)Math.Pow(continuousErrorsCount, 3), (int)Math.Pow(continuousErrorsCount + 1, 3) + 1),
                maxSleepMs);

            if (Log.IsDebugEnabled)
                Log.DebugFormat("Sleeping for {0}ms after {1} continuous errors", nextTry, continuousErrorsCount);

            TaskUtils.Sleep(nextTry);
        }

        public static class Operation //dep-free copy of WorkerOperation
        {
            public const int NoOp = 0;
            public const int Stop = 1;
            public const int Reset = 2;
            public const int Restart = 3;

            public static string GetName(int op)
            {
                switch (op)
                {
                    case NoOp:
                        return "NoOp";
                    case Stop:
                        return "Stop";
                    case Reset:
                        return "Reset";
                    case Restart:
                        return "Restart";
                    default:
                        return null;
                }
            }
        }

        public static class ControlCommand
        {
            public const string Control = "CTRL";
            public const string Pulse = "PULSE";
        }

        class Status //dep-free copy of WorkerStatus
        {
            public const int Disposed = -1;
            public const int Stopped = 0;
            public const int Stopping = 1;
            public const int Starting = 2;
            public const int Started = 3;

            public static string GetStatus(int status)
            {
                return status switch {
                    Disposed => nameof(Disposed),
                    Stopped => nameof(Stopped),
                    Stopping => nameof(Stopping),
                    Starting => nameof(Starting),
                    Started => nameof(Started),
                    _ => throw new NotSupportedException("Unknown status: " + status)
                };
            }
        }

        public string GetStatus() => Status.GetStatus(Interlocked.CompareExchange(ref status, 0, 0));

        public string GetStatsDescription()
        {
            var sb = StringBuilderCache.Allocate();
            sb.AppendLine("===============");
            sb.AppendLine("Current Status: " + GetStatus());
            sb.AppendLine("Times Started: " + Interlocked.CompareExchange(ref timesStarted, 0, 0));
            sb.AppendLine("Num of Errors: " + Interlocked.CompareExchange(ref noOfErrors, 0, 0));
            sb.AppendLine("Num of Continuous Errors: " + Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));
            sb.AppendLine("Last ErrorMsg: " + lastExMsg);
            sb.AppendLine("===============");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public virtual void Dispose()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == Status.Disposed)
                return;
            
            if (Log.IsDebugEnabled)
                Log.Debug("RedisPubServer.Dispose()...");

            OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {GetStatus()}] Dispose()>");

            Stop();

            var holdStatus = GetStatus();
            
            if (Interlocked.CompareExchange(ref status, Status.Disposed, Status.Stopped) != Status.Stopped)
                Interlocked.CompareExchange(ref status, Status.Disposed, Status.Stopping);

            OnEvent?.Invoke($"[{DateTime.UtcNow.TimeOfDay:g} {holdStatus}] Dispose()> -> Disposed");

            try
            {
                OnDispose?.Invoke();
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
                OnError?.Invoke(ex);
            }

            DisposeHeartbeatTimer();
        }
    }
}
