using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientConfigTests
        : RedisClientTestsBase
    {
        [Ignore("Hurts MSOpenTech Redis Server")]
        [Test]
        public void Can_Set_and_Get_Config()
        {
            var orig = Redis.GetConfig("maxmemory");
            var newMaxMemory = (long.Parse(orig) + 1).ToString();
            Redis.SetConfig("maxmemory", newMaxMemory);
            var current = Redis.GetConfig("maxmemory");
            Assert.That(current, Is.EqualTo(newMaxMemory));
        }

        [Test]
        public void Can_Rewrite_Redis_Config()
        {
            try
            {
                Redis.SaveConfig();
            }
            catch (RedisResponseException ex)
            {
                if (ex.Message.StartsWith("Rewriting config file: Permission denied")
                    || ex.Message.StartsWith("The server is running without a config file"))
                    return;
                throw;
            }
        }

        [Test]
        public void Can_Rewrite_Info_Stats()
        {
            Redis.ResetInfoStats();
        }

        [Test]
        public void Can_set_and_Get_Client_Name()
        {
            var clientName = "CLIENT-" + Environment.TickCount;
            Redis.SetClient(clientName);
            var client = Redis.GetClient();

            Assert.That(client, Is.EqualTo(clientName));
        }

        [Test]
        public void Can_GetClientsInfo()
        {
            var clientList = Redis.GetClientsInfo();
            clientList.PrintDump();
        }

        [Test]
        public void Can_Kill_Client()
        {
            var clientList = Redis.GetClientsInfo();
            var firstAddr = clientList.First()["addr"];
            Redis.KillClient(firstAddr);
        }

        [Test]
        public void Can_Kill_Clients()
        {
            Redis.KillClients(fromAddress: "192.168.0.1:6379");
            Redis.KillClients(withId: "1");
            Redis.KillClients(ofType: RedisClientType.Normal);
            Redis.KillClients(ofType: RedisClientType.PubSub);
            Redis.KillClients(ofType: RedisClientType.Slave);
            Redis.KillClients(skipMe: true);
            Redis.KillClients(fromAddress: "192.168.0.1:6379", withId: "1", ofType: RedisClientType.Normal);
            Redis.KillClients(skipMe: false);
        }

        [Test]
        public void Can_get_Role_Info()
        {
            var result = Redis.Role();
            result.PrintDump();
            Assert.That(result.Children[0].Text, Is.EqualTo("master"));
            Assert.That(Redis.GetServerRole(), Is.EqualTo(RedisServerRole.Master));

            //needs redis-server v3.0
            //var replica = new RedisClient("10.0.0.9:6380");
            //result = replica.Role();
            //result.PrintDump();
        }

        [Test]
        public void Can_PauseAllClients()
        {
            //needs redis-server v3.0
            //var replica = new RedisClient("10.0.0.9:6380");
            //replica.PauseAllClients(TimeSpan.FromSeconds(2));
        }
    }
}