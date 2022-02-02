using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class ManagedListGenericTests
    {
        private IRedisClientsManager redisManager;

        [SetUp]
        public void TestSetUp()
        {
            if (redisManager != null) redisManager.Dispose();
            redisManager = TestConfig.BasicClientManger;
            redisManager.Exec(r => r.FlushAll());
        }

        private ManagedList<string> GetManagedList()
        {
            return redisManager.GetManagedList<string>("testkey");
        }

        [Test]
        public void Can_Get_Managed_List()
        {
            var managedList = GetManagedList();

            var testString = "simple Item to test";
            managedList.Add(testString);

            var actualList = GetManagedList();

            Assert.AreEqual(managedList.First(), actualList.First());
        }
    }
}
