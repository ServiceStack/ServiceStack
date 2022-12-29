using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientConfigTestsAsync
        : RedisClientTestsBaseAsync
    {
        [Ignore("Hurts MSOpenTech Redis Server")]
        [Test]
        public async Task Can_Set_and_Get_Config()
        {
            var orig = await RedisAsync.GetConfigAsync("maxmemory");
            var newMaxMemory = (long.Parse(orig) + 1).ToString();
            await RedisAsync.SetConfigAsync("maxmemory", newMaxMemory);
            var current = await RedisAsync.GetConfigAsync("maxmemory");
            Assert.That(current, Is.EqualTo(newMaxMemory));
        }

        [Test]
        public async Task Can_Rewrite_Redis_Config()
        {
            try
            {
                await RedisAsync.SaveConfigAsync();
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
        public async Task Can_Rewrite_Info_Stats()
        {
            await RedisAsync.ResetInfoStatsAsync();
        }

        [Test]
        public async Task Can_set_and_Get_Client_Name()
        {
            var clientName = "CLIENT-" + Environment.TickCount;
            await RedisAsync.SetClientAsync(clientName);
            var client = await RedisAsync.GetClientAsync();

            Assert.That(client, Is.EqualTo(clientName));
        }

        [Test]
        public async Task Can_GetClientsInfo()
        {
            var clientList = await RedisAsync.GetClientsInfoAsync();
            clientList.PrintDump();
        }

        [Test]
        public async Task Can_Kill_Client()
        {
            var clientList = await RedisAsync.GetClientsInfoAsync();
            var firstAddr = clientList.First()["addr"];
            await RedisAsync.KillClientAsync(firstAddr);
        }

        [Test]
        public async Task Can_Kill_Clients()
        {
            await RedisAsync.KillClientsAsync(fromAddress: "192.168.0.1:6379");
            await RedisAsync.KillClientsAsync(withId: "1");
            await RedisAsync.KillClientsAsync(ofType: RedisClientType.Normal);
            await RedisAsync.KillClientsAsync(ofType: RedisClientType.PubSub);
            await RedisAsync.KillClientsAsync(ofType: RedisClientType.Slave);
            await RedisAsync.KillClientsAsync(skipMe: true);
            await RedisAsync.KillClientsAsync(fromAddress: "192.168.0.1:6379", withId: "1", ofType: RedisClientType.Normal);
            await RedisAsync.KillClientsAsync(skipMe: false);
        }

        [Test]
        public async Task Can_get_Role_Info()
        {
            var result = await NativeAsync.RoleAsync();
            result.PrintDump();
            Assert.That(result.Children[0].Text, Is.EqualTo("master"));
            Assert.That(await RedisAsync.GetServerRoleAsync(), Is.EqualTo(RedisServerRole.Master));

            //needs redis-server v3.0
            //var replica = new RedisClient("10.0.0.9:6380");
            //result = replica.Role();
            //result.PrintDump();
        }

        [Test]
        public Task Can_PauseAllClients()
        {
            //needs redis-server v3.0
            //var replica = new RedisClient("10.0.0.9:6380");
            //replica.PauseAllClients(TimeSpan.FromSeconds(2));

            return Task.CompletedTask;
        }
    }
}