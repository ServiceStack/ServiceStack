using System;
using System.IO;
using System.Threading;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqWorker : IMqWorker<SqsMqWorker>
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SqsMqWorker));

        private readonly object _msgLock = new object();
        private readonly ISqsMqMessageFactory mqFactory;
        private readonly SqsMqWorkerInfo queueWorkerInfo;
        private readonly IMessageHandler messageHandler;
        private readonly Action<SqsMqWorker, Exception> errorHandler;

        private IMessageQueueClient mqClient;
        private Thread bgThread;
        private int status;
        private int totalMessagesProcessed;

        public TimeSpan PollingDuration { get; set; } = TimeSpan.FromMilliseconds(1000);
        
        public SqsMqWorker(ISqsMqMessageFactory mqFactory,
                           SqsMqWorkerInfo queueWorkerInfo,
                           string queueName,
                           Action<SqsMqWorker, Exception> errorHandler)
        {
            Guard.AgainstNullArgument(mqFactory, "mqFactory");
            Guard.AgainstNullArgument(queueWorkerInfo, "queueWorkerInfo");
            Guard.AgainstNullArgument(queueName, "queueName");
            Guard.AgainstNullArgument(queueWorkerInfo.MessageHandlerFactory, "queueWorkerInfo.MessageHandlerFactory");

            this.mqFactory = mqFactory;
            this.queueWorkerInfo = queueWorkerInfo;
            this.errorHandler = errorHandler;
            messageHandler = this.queueWorkerInfo.MessageHandlerFactory.CreateMessageHandler();
            QueueName = queueName;
        }
        
        public string QueueName { get; set; }

        public int TotalMessagesProcessed
        {
            get { return totalMessagesProcessed; }
        }

        public IMessageQueueClient MqClient
        {
            get { return mqClient ?? (mqClient = mqFactory.CreateMessageQueueClient()); }
        }

        public SqsMqWorker Clone()
        {
            return new SqsMqWorker(mqFactory, queueWorkerInfo, QueueName, errorHandler);
        }

        public IMessageHandlerStats GetStats()
        {
            return messageHandler.GetStats();
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                return;

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("MQ Host has been disposed");

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Stopping)
            {
                KillBgThreadIfExists();
            }

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped)
            {
                log.Debug($"Starting MQ Handler Worker: {QueueName}...");

                //Should only be 1 thread past this point
                bgThread = new Thread(Run)
                {
                    Name = $"{GetType().Name}: {QueueName}",
                    IsBackground = true,
                };

                bgThread.Start();
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                log.Debug($"Stopping SQS MQ Handler Worker: {QueueName}...");
                
                Thread.Sleep(100);
                
                lock(_msgLock)
                {
                    Monitor.Pulse(_msgLock);
                }
                
                DisposeMqClient();
            }
        }

        private void Run()
        {
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting)
                return;

            try
            {
                lock(_msgLock)
                {
                    StartPolling();
                }
            }
#if !NETCORE            
            catch(ThreadInterruptedException)
            {   // Expected exceptions from Kill()
                log.Warn($"Received ThreadInterruptedException in Worker: {QueueName}");
            }
            catch(ThreadAbortException)
            {   // Expected exceptions from Kill()
                log.Warn($"Received ThreadAbortException in Worker: {QueueName}");
            }
#endif
            catch (Exception ex)
            {   
                Stop();

                errorHandler?.Invoke(this, ex);
            }
            finally
            {
                try
                {
                    DisposeMqClient();
                }
                catch { }

                // If in an invalid state, Dispose() this worker.
                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping) != WorkerStatus.Stopping)
                {
                    Dispose();
                }

                bgThread = null;
            }
        }

        private void StartPolling()
        {
            var retryCount = 0;

            while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
            {
                try
                {
                    var msgsProcessedThisTime = messageHandler.ProcessQueue(MqClient,
                        QueueName,
                        () => Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started);

                    totalMessagesProcessed += msgsProcessedThisTime;
                    
                    Monitor.Wait(_msgLock, timeout: PollingDuration);

                    retryCount = 0;
                }
                catch(EndOfStreamException)
                {
                    throw;
                }
                catch (Exception ex)
                {   
                    if (Interlocked.CompareExchange(ref status, 0, 0) != WorkerStatus.Started)
                    {   // No longer supposed to be running...
                        return;
                    }

                    log.Debug($"Received exception polling in MqWorker {QueueName}", ex);

                    // If it was an unexpected exception, pause for a bit before retrying
                    var waitMs = Math.Min(retryCount++ * 2000, 60000);
                    log.Debug($"Retrying poll after {waitMs}ms...", ex);
                    Thread.Sleep(waitMs);
                }
            }
        }

        private void KillBgThreadIfExists()
        {
            try
            {
                if (bgThread == null || !bgThread.IsAlive)
                    return;

                if (!bgThread.Join(10000))
                {
                    log.Warn(string.Concat("Interrupting previous Background Worker: ", bgThread.Name));

                    bgThread.Interrupt();

                    if (!bgThread.Join(TimeSpan.FromSeconds(10)))
                    {
                        log.Warn(string.Concat(bgThread.Name, " just wont die, so we're now aborting it..."));
#pragma warning disable CS0618, SYSLIB0014, SYSLIB0006
                        bgThread.Abort();
#pragma warning restore CS0618, SYSLIB0014, SYSLIB0006
                    }
                }

            }
            finally
            {
                bgThread = null;
                Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, status);
            }
        }

        private void DisposeMqClient()
        {
            // Disposing mqClient causes an EndOfStreamException to be thrown in StartSubscription
            if (mqClient == null)
                return;

            mqClient.Dispose();
            mqClient = null;
        }

        public virtual void Dispose()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            Stop();

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopped) != WorkerStatus.Stopped)
            {
                Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopping);
            }

            try
            {
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                log.Error(string.Concat("Error Disposing MessageHandlerWorker for: ", QueueName), ex);
            }
        }
    }

}