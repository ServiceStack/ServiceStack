using System;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqWorker : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RabbitMqWorker));

        readonly object msgLock = new object();

        private readonly RabbitMqMessageFactory mqFactory;
        private IMessageQueueClient mqClient;
        private readonly IMessageHandler messageHandler;
        public string QueueName { get; set; }

        private int status;
        public int Status
        {
            get { return status; }
        }

        private Thread bgThread;
        private int timesStarted = 0;
        private bool receivedNewMsgs = false;
        public int SleepTimeoutMs = 5000;
        public Action<RabbitMqWorker, Exception> errorHandler { get; set; }

        private DateTime lastMsgProcessed;
        public DateTime LastMsgProcessed
        {
            get { return lastMsgProcessed; }
        }

        private int totalMessagesProcessed;
        public int TotalMessagesProcessed
        {
            get { return totalMessagesProcessed; }
        }

        private int msgNotificationsReceived;
        public int MsgNotificationsReceived
        {
            get { return msgNotificationsReceived; }
        }

        public RabbitMqWorker(RabbitMqMessageFactory mqFactory,
                              IMessageHandler messageHandler, string queueName,
                              Action<RabbitMqWorker, Exception> errorHandler)
        {
            this.mqFactory = mqFactory;
            this.messageHandler = messageHandler;
            this.QueueName = queueName;
            this.errorHandler = errorHandler;
        }

        public RabbitMqWorker Clone()
        {
            return new RabbitMqWorker(mqFactory, messageHandler, QueueName, errorHandler);
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                return;
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("MQ Host has been disposed");
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Stopping)
                KillBgThreadIfExists();

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped)
            {
                Log.Debug("Starting MQ Handler Worker: {0}...".Fmt(QueueName));

                //Should only be 1 thread past this point
                bgThread = new Thread(Run)
                {
                    Name = "{0}: {1}".Fmt(GetType().Name, QueueName),
                    IsBackground = true,
                };
                bgThread.Start();
            }
        }

        public void ForceRestart()
        {
            KillBgThreadIfExists();
            Start();
        }

        private void Run()
        {
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting) return;
            timesStarted++;

            try
            {
                lock (msgLock)
                {
                    mqClient = mqFactory.CreateMessageQueueClient();

                    while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                    {
                        receivedNewMsgs = false;

                        var msgsProcessedThisTime = messageHandler.ProcessQueue(
                            mqClient, QueueName,
                            () => Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started);

                        totalMessagesProcessed += msgsProcessedThisTime;

                        if (msgsProcessedThisTime > 0)
                            lastMsgProcessed = DateTime.UtcNow;

                        if (!receivedNewMsgs)
                            Monitor.Wait(msgLock, millisecondsTimeout: SleepTimeoutMs);
                    }
                }
            }
            catch (Exception ex)
            {
                //Ignore handling rare, but expected exceptions from KillBgThreadIfExists()
                if (ex is ThreadInterruptedException || ex is ThreadAbortException)
                {
                    Log.Warn("Received {0} in Worker: {1}".Fmt(ex.GetType().Name, QueueName));
                    return;
                }

                Stop();
                if (this.errorHandler != null) this.errorHandler(this, ex);
            }
            finally
            {
                try
                {
                    mqClient.Dispose();
                    mqClient = null;
                }
                catch {}

                //If it's in an invalid state, Dispose() this worker.
                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping) != WorkerStatus.Stopping)
                {
                    Dispose();
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                Log.Debug("Stopping MQ Handler Worker: {0}...".Fmt(QueueName));
                Thread.Sleep(100);
                lock (msgLock)
                {
                    Monitor.Pulse(msgLock);
                }
            }
        }

        private void KillBgThreadIfExists()
        {
            try
            {
                if (bgThread != null && bgThread.IsAlive)
                {
                    //give it a small chance to die gracefully
                    if (!bgThread.Join(500))
                    {
                        //Ideally we shouldn't get here, but lets try our hardest to clean it up
                        Log.Warn("Interrupting previous Background Worker: " + bgThread.Name);
                        bgThread.Interrupt();
                        if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                        {
                            Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
                            bgThread.Abort();
                        }
                    }
                }
            }
            finally
            {
                bgThread = null;
                status = WorkerStatus.Stopped;
            }
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
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                Log.Error("Error Disposing MessageHandlerWorker for: " + QueueName, ex);
            }
        }

        public IMessageHandlerStats GetStats()
        {
            return messageHandler.GetStats();
        }

        public string GetStatus()
        {
            return "[Worker: {0}, Status: {1}, ThreadStatus: {2}, LastMsgAt: {3}]"
                .Fmt(QueueName, WorkerStatus.ToString(status), bgThread.ThreadState, LastMsgProcessed);
        }
    }
}