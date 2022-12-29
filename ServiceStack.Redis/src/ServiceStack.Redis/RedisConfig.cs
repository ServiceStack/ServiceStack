using System;
using System.Net.Security;
using System.Threading;

namespace ServiceStack.Redis
{
    public class RedisConfig
    {
        //redis-server defaults:
        public const long DefaultDb = 0;
        public const int DefaultPort = 6379;
        public const int DefaultPortSsl = 6380;
        public const int DefaultPortSentinel = 26379;
        public const string DefaultHost = "localhost";

        /// <summary>
        /// Factory used to Create `RedisClient` instances
        /// </summary>
        public static Func<RedisEndpoint, RedisClient> ClientFactory = c =>
        {
            Interlocked.Increment(ref RedisState.TotalClientsCreated);
            return new RedisClient(c);
        };

        /// <summary>
        /// The default RedisClient Socket ConnectTimeout (default -1, None)
        /// </summary>
        public static int DefaultConnectTimeout = -1;

        /// <summary>
        /// The default RedisClient Socket SendTimeout (default -1, None)
        /// </summary>
        public static int DefaultSendTimeout = -1;

        /// <summary>
        /// The default RedisClient Socket ReceiveTimeout (default -1, None)
        /// </summary>
        public static int DefaultReceiveTimeout = -1;

        /// <summary>
        /// Default Idle TimeOut before a connection is considered to be stale (default 240 secs)
        /// </summary>
        public static int DefaultIdleTimeOutSecs = 240;

        /// <summary>
        /// The default RetryTimeout for auto retry of failed operations (default 10,000ms)
        /// </summary>
        public static int DefaultRetryTimeout = 10 * 1000;

        /// <summary>
        /// Default Max Pool Size for Pooled Redis Client Managers (default none)
        /// </summary>
        public static int? DefaultMaxPoolSize;

        /// <summary>
        /// The default pool size multiplier if no pool size is specified (default 50)
        /// </summary>
        public static int DefaultPoolSizeMultiplier = 50;

        /// <summary>
        /// The BackOff multiplier failed Auto Retries starts from (default 10ms)
        /// </summary>
        public static int BackOffMultiplier = 10;

        /// <summary>
        /// The Byte Buffer Size to combine Redis Operations within (1450 bytes)
        /// </summary>
        public static int BufferLength => ServiceStack.Text.Pools.BufferPool.BUFFER_LENGTH;

        /// <summary>
        /// The Byte Buffer Size for Operations to use a byte buffer pool (default 500kb)
        /// </summary>
        public static int BufferPoolMaxSize = 500000;

        /// <summary>
        /// Batch size of keys to include in a single Redis Command (e.g. DEL k1 k2...) 
        /// </summary>
        public static int CommandKeysBatchSize = 10000;

        /// <summary>
        /// Whether Connections to Master hosts should be verified they're still master instances (default true)
        /// </summary>
        public static bool VerifyMasterConnections = true;

        /// <summary>
        /// Whether to retry re-connecting on same connection if not a master instance (default true)
        /// For Managed Services (e.g. AWS ElastiCache) which eventually restores master instances on same host
        /// </summary>
        public static bool RetryReconnectOnFailedMasters = true;

        /// <summary>
        /// The ConnectTimeout on clients used to find the next available host (default 200ms)
        /// </summary>
        public static int HostLookupTimeoutMs = 200;

        /// <summary>
        /// Skip ServerVersion Checks by specifying Min Version number, e.g: 2.8.12 => 2812, 2.9.1 => 2910
        /// </summary>
        public static int? AssumeServerVersion;

        /// <summary>
        /// How long to hold deactivated clients for before disposing their connection (default 0 seconds)
        /// Dispose of deactivated Clients immediately with TimeSpan.Zero
        /// </summary>
        public static TimeSpan DeactivatedClientsExpiry = TimeSpan.Zero;

        /// <summary>
        /// Whether Debug Logging should log detailed Redis operations (default false)
        /// </summary>
        public static bool EnableVerboseLogging = false;

        [Obsolete("Use EnableVerboseLogging")]
        public static bool DisableVerboseLogging
        {
            get => !EnableVerboseLogging;
            set => EnableVerboseLogging = !value;
        }

        //Example at: http://msdn.microsoft.com/en-us/library/office/dd633677(v=exchg.80).aspx 
        public static LocalCertificateSelectionCallback CertificateSelectionCallback { get; set; }
        public static RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        /// Assert all access using pooled RedisClient instance should be limited to same thread.
        /// Captures StackTrace so is very slow, use only for debugging connection issues.
        /// </summary>
        public static bool AssertAccessOnlyOnSameThread = false;

        /// <summary>
        /// Resets Redis Config and Redis Stats back to default values
        /// </summary>
        public static void Reset()
        {
            RedisStats.Reset();

            DefaultConnectTimeout = -1;
            DefaultSendTimeout = -1;
            DefaultReceiveTimeout = -1;
            DefaultRetryTimeout = 10 * 1000;
            DefaultIdleTimeOutSecs = 240;
            DefaultMaxPoolSize = null;
            BackOffMultiplier = 10;
            BufferPoolMaxSize = 500000;
            CommandKeysBatchSize = 10000;
            VerifyMasterConnections = true;
            RetryReconnectOnFailedMasters = true;
            HostLookupTimeoutMs = 200;
            AssumeServerVersion = null;
            DeactivatedClientsExpiry = TimeSpan.Zero;
            EnableVerboseLogging = false;
            CertificateSelectionCallback = null;
            CertificateValidationCallback = null;
            AssertAccessOnlyOnSameThread = false;
        }
    }
}