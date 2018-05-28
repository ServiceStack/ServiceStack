using System;
using System.IO;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ServiceStack.Logging;
using ServiceStack.Messaging;

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
        public bool AutoReconnect { get; set; }

        private int status;
        public int Status => status;

        private Thread bgThread;
        private int timesStarted = 0;
        private bool receivedNewMsgs = false;
        public int SleepTimeoutMs = 1000;
        public Action<RabbitMqWorker, Exception> errorHandler { get; set; }

        private DateTime lastMsgProcessed;
        public DateTime LastMsgProcessed => lastMsgProcessed;

        private int totalMessagesProcessed;
        public int TotalMessagesProcessed => totalMessagesProcessed;

        // TODO: RabbitMqWorker.MsgNotificationsReceived is never referenced and will always return zero.
        //private int msgNotificationsReceived;
        public int MsgNotificationsReceived => 0;

        public RabbitMqWorker(RabbitMqMessageFactory mqFactory,
            IMessageHandler messageHandler, string queueName,
            Action<RabbitMqWorker, Exception> errorHandler,
            bool autoConnect = true)
        {
            this.mqFactory = mqFactory;
            this.messageHandler = messageHandler;
            this.QueueName = queueName;
            this.errorHandler = errorHandler;
            this.AutoReconnect = autoConnect;
        }

        public virtual RabbitMqWorker Clone()
        {
            return new RabbitMqWorker(mqFactory, messageHandler, QueueName, errorHandler, AutoReconnect);
        }

        public IMessageQueueClient MqClient => mqClient ?? (mqClient = mqFactory.CreateMessageQueueClient());

        private IModel GetChannel()
        {
            var rabbitClient = (RabbitMqQueueClient)MqClient;
            var channel = rabbitClient.Channel;
            return channel;
        }

        public virtual void Start()
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
                    Name = $"{GetType().Name}: {QueueName}",
                    IsBackground = true,
                };
                bgThread.Start();
            }
        }

        public virtual void ForceRestart()
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
                if (mqFactory.UsePolling)
                {
                    lock (msgLock)
                    {
                        StartPolling();
                    }
                }
                else
                {
                    StartSubscription();
                }
            }
            catch (Exception ex)
            {
#if !NETSTANDARD2_0
                //Ignore handling rare, but expected exceptions from KillBgThreadIfExists()
                if (ex is ThreadInterruptedException || ex is ThreadAbortException)
                {
                    Log.Warn("Received {0} in Worker: {1}".Fmt(ex.GetType().Name, QueueName));
                    return;
                }
#endif

                Stop();

                errorHandler?.Invoke(this, ex);
            }
            finally
            {
                try
                {
                    DisposeMqClient();
                }
                catch {}

                //If it's in an invalid state, Dispose() this worker.
                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping) != WorkerStatus.Stopping)
                {
                    Dispose();
                }
                //status is either 'Stopped' or 'Disposed' at this point

                bgThread = null;
            }
        }

        private void StartPolling()
        {
            while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
            {
                try
                {
                    receivedNewMsgs = false;

                    var msgsProcessedThisTime = messageHandler.ProcessQueue(
                        MqClient, QueueName,
                        () => Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started);

                    totalMessagesProcessed += msgsProcessedThisTime;

                    if (msgsProcessedThisTime > 0)
                        lastMsgProcessed = DateTime.UtcNow;

                    if (!receivedNewMsgs)
                        Monitor.Wait(msgLock, millisecondsTimeout: SleepTimeoutMs);
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationInterruptedException
                        || ex is EndOfStreamException))
                        throw;

                    //The consumer was cancelled, the model or the connection went away.
                    if (Interlocked.CompareExchange(ref status, 0, 0) != WorkerStatus.Started
                        || !AutoReconnect)
                        return;

                    //If it was an unexpected exception, try reconnecting
                    WaitForReconnect();
                }
            }
        }

        private IModel WaitForReconnect()
        {
            var retries = 1;
            while (true)
            {
                DisposeMqClient();
                try
                {
                    var channel = GetChannel();
                    return channel;
                }
                catch (Exception ex)
                {
                    var waitMs = Math.Min(retries++ * 100, 10000);
                    Log.Debug("Retrying to Reconnect after {0}ms...".Fmt(waitMs), ex);
                    Thread.Sleep(waitMs);
                }
            }
        }

        private void StartSubscription()
        {
            var consumer = ConnectSubscription();

            // At this point, messages will be being asynchronously delivered,
            // and will be queueing up in consumer.Queue.
            while (true)
            {
                try
                {
                    var e = consumer.Queue.Dequeue();

                    mqFactory.GetMessageFilter?.Invoke(QueueName, e);

                    messageHandler.ProcessMessage(mqClient, e);
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationInterruptedException
                        || ex is EndOfStreamException))
                        throw;

                    // The consumer was cancelled, the model closed, or the connection went away.
                    if (Interlocked.CompareExchange(ref status, 0, 0) != WorkerStatus.Started
                        || !AutoReconnect)
                        return;

                    //If it was an unexpected exception, try reconnecting
                    consumer = WaitForReconnectSubscription();
                }
            }
        }

        private RabbitMqBasicConsumer WaitForReconnectSubscription()
        {
            var retries = 1;
            while (true)
            {
                DisposeMqClient();
                try
                {
                    return ConnectSubscription();
                }
                catch (Exception ex)
                {
                    var waitMs = Math.Min(retries++ * 100, 10000);
                    Log.Debug("Retrying to Reconnect Subscription after {0}ms...".Fmt(waitMs), ex);
                    Thread.Sleep(waitMs);
                }
            }
        }

        private RabbitMqBasicConsumer ConnectSubscription()
        {
            var channel = GetChannel();
            var consumer = new RabbitMqBasicConsumer(channel);
            channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);
            return consumer;
        }

        public virtual void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                Log.Debug("Stopping Rabbit MQ Handler Worker: {0}...".Fmt(QueueName));
                if (mqFactory.UsePolling)
                {
                    Thread.Sleep(100);
                    lock (msgLock)
                    {
                        Monitor.Pulse(msgLock);
                    }
                }

                DisposeMqClient();
            }
        }

        private void DisposeMqClient()
        {
            //Disposing mqClient causes an EndOfStreamException to be thrown in StartSubscription
            if (mqClient == null) return;
            mqClient.Dispose();
            mqClient = null;
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
                Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, status);
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

        public virtual IMessageHandlerStats GetStats()
        {
            return messageHandler.GetStats();
        }

        public virtual string GetStatus()
        {
            return "[Worker: {0}, Status: {1}, ThreadStatus: {2}, LastMsgAt: {3}]"
                .Fmt(QueueName, WorkerStatus.ToString(status), bgThread.ThreadState, LastMsgProcessed);
        }
    }
}