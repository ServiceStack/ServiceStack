using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Templates;

namespace ServiceStack.Redis.Tests
{
    class RedisTemplateTests
    {
        [Test]
        public void Does_build_connection_string()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new RedisScripts() }
            };
            context.Container.AddSingleton<IRedisClientsManager>(() => new RedisManagerPool());
            context.Init();

            Assert.That(context.EvaluateScript("{{ redisToConnectionString: host:7000?db=1 }}"),
                Is.EqualTo("host:7000?db=1"));

            Assert.That(context.EvaluateScript("{{ { host: 'host' } | redisToConnectionString }}"),
                Is.EqualTo("host:6379?db=0"));

            Assert.That(context.EvaluateScript("{{ { port: 7000 } | redisToConnectionString }}"),
                Is.EqualTo("localhost:7000?db=0"));

            Assert.That(context.EvaluateScript("{{ { db: 1 } | redisToConnectionString }}"),
                Is.EqualTo("localhost:6379?db=1"));

            Assert.That(context.EvaluateScript("{{ { host: 'host', port: 7000, db: 1 } | redisToConnectionString }}"),
                Is.EqualTo("host:7000?db=1"));

            Assert.That(context.EvaluateScript("{{ { host: 'host', port: 7000, db: 1, password:'secret' } | redisToConnectionString | raw }}"),
                Is.EqualTo("host:7000?db=1&password=secret"));

            Assert.That(context.EvaluateScript("{{ redisConnectionString }}"),
                Is.EqualTo("localhost:6379?db=0"));

            Assert.That(context.EvaluateScript("{{ { db: 1 } | redisChangeConnection }}"),
                Is.EqualTo("localhost:6379?db=1"));

            Assert.That(context.EvaluateScript("{{ redisConnectionString }}"),
                Is.EqualTo("localhost:6379?db=1"));
        }
    }
}
