using NUnit.Framework;
using System.Diagnostics;
using System.Text;

namespace ServiceStack.Redis.Tests
{
    public class RedisClientTestsBase
    {
        protected string CleanMask = null;
        protected RedisClient Redis;

        protected void Log(string fmt, params object[] args)
        {
            Debug.WriteLine("{0}", string.Format(fmt, args).Trim());
        }

        [OneTimeSetUp]
        public virtual void OnBeforeTestFixture()
        {
            RedisClient.NewFactoryFn = () => new RedisClient(TestConfig.SingleHost);
            using (var redis = RedisClient.New())
            {
                redis.FlushAll();
            }
        }

        [OneTimeTearDown]
        public virtual void OnAfterTestFixture()
        {
        }

        [SetUp]
        public virtual void OnBeforeEachTest()
        {
            Redis = RedisClient.New();
        }

        [TearDown]
        public virtual void OnAfterEachTest()
        {
            try
            {
                if (Redis.NamespacePrefix != null && CleanMask == null) CleanMask = Redis.NamespacePrefix + "*";
                if (CleanMask != null) Redis.SearchKeys(CleanMask).ForEach(t => Redis.Del(t));
                Redis.Dispose();
            }
            catch (RedisResponseException e)
            {
                // if exception has that message then it still proves that BgSave works as expected.
                if (e.Message.StartsWith("Can't BGSAVE while AOF log rewriting is in progress"))
                    return;

                throw;
            }
        }

        protected string PrefixedKey(string key)
        {
            return string.Concat(Redis.NamespacePrefix, key);
        }

        public RedisClient GetRedisClient()
        {
            var client = new RedisClient(TestConfig.SingleHost);
            return client;
        }

        public RedisClient CreateRedisClient()
        {
            var client = new RedisClient(TestConfig.SingleHost);
            return client;
        }

        public string GetString(byte[] stringBytes)
        {
            return Encoding.UTF8.GetString(stringBytes);
        }

        public byte[] GetBytes(string stringValue)
        {
            return Encoding.UTF8.GetBytes(stringValue);
        }
    }
}