namespace ConsoleTests
{
    public class Incr
    {
        public long Id { get; set; }
    }

    public class IncrResponse
    {
        public long Result { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //new LongRunningRedisPubSubServer().Execute("10.0.0.9");
            //new HashStressTest().Execute("127.0.0.1");
            //new HashStressTest().Execute("10.0.0.9");
            //new HashCollectionStressTests().Execute("10.0.0.9", noOfThreads: 64);

            //new LocalRedisSentinelFailoverTests
            //{
            //    StartAndStopRedisServers = true
            //}.Execute();

            //new LocalRedisSentinelFailoverTests {
            //    UseRedisManagerPool = true, StartAndStopRedisServers = false }.Execute();
            //new LocalRedisSentinelFailoverTests().Execute();

            //new NetworkRedisSentinelFailoverTests().Execute();

            //new GoogleRedisSentinelFailoverTests().Execute();

            //new ForceFailover().Execute();

            //new BlockingPop().Execute();

            //new MasterFailoverWithPassword().Execute();
            
            //new BlockingRemoveAfterReconnection().Execute();
            
            //new MultiBlockingRemoveAfterReconnection().Execute();
            
            new DbSelectConnectionStringIssue().Execute();
        }
    }
}
