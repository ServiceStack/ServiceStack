using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientsManagerExtensionsTests
    {
        private IRedisClientsManager redisManager;

        [SetUp]
        public void OnBeforeEachTest()
        {
            if (redisManager != null) redisManager.Dispose();
            redisManager = TestConfig.BasicClientManger;
            redisManager.Exec(r => r.FlushAll());
        }

        [Test]
        public void Can_Exec_Action()
        {
            redisManager.Exec(r =>
            {
                r.Increment("key", 1);
                Assert.That(r.Get<int>("key"), Is.EqualTo(1));
            });
        }

        [Test]
        public void Can_Exec_Func_string()
        {
            string value = redisManager.Exec(r =>
            {
                r.SetValue("key", "value");
                return r.GetValue("key");
            });
            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public void Can_Exec_Func_long()
        {
            long value = redisManager.Exec(r => r.Increment("key", 1));
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_Exec_Func_int()
        {
            long value = redisManager.Exec(r =>
            {
                r.AddItemToList("list", "value");
                return r.GetListCount("list");
            });
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Can_Exec_Func_double()
        {
            double value = redisManager.Exec(r =>
            {
                r.AddItemToSortedSet("zset", "value", 1.1d);
                return r.GetItemScoreInSortedSet("zset", "value");
            });

            Assert.That(value, Is.EqualTo(1.1d));
        }

        [Test]
        public void Can_Exec_Func_bool()
        {
            bool value = redisManager.Exec(r =>
            {
                r.AddItemToSet("set", "item");
                return r.SetContainsItem("set", "item");
            });

            Assert.That(value, Is.True);
        }

        [Test]
        public void Can_Exec_Transaction_Action()
        {
            var value = false;
            redisManager.ExecTrans(trans =>
            {
                trans.QueueCommand(r => r.AddItemToSet("set", "item"));
                trans.QueueCommand(r => r.SetContainsItem("set", "item"), x => value = x);
            });

            Assert.That(value, Is.True);
        }

        [Test]
        public void Can_ExecAs_ModelWithIdAndName_Action()
        {
            var expected = ModelWithIdAndName.Create(1);
            redisManager.ExecAs<ModelWithIdAndName>(m =>
            {
                m.Store(expected);
                var actual = m.GetById(expected.Id);
                Assert.That(actual, Is.EqualTo(expected));
            });
        }

        [Test]
        public void Can_ExecAs_ModelWithIdAndName_Func()
        {
            var expected = ModelWithIdAndName.Create(1);
            ModelWithIdAndName actual = redisManager.ExecAs<ModelWithIdAndName>(m =>
            {
                m.Store(expected);
                return m.GetById(expected.Id);
            });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Can_ExecAs_ModelWithIdAndName_Func_IList()
        {
            var expected = new[] {
                   ModelWithIdAndName.Create(1),
                   ModelWithIdAndName.Create(2),
                   ModelWithIdAndName.Create(3),
               };
            IList<ModelWithIdAndName> actual = redisManager.ExecAs<ModelWithIdAndName>(m =>
            {
                var list = m.Lists["typed-list"];
                list.AddRange(expected);
                return (IList<ModelWithIdAndName>)list.GetAll();
            });
            Assert.That(actual.EquivalentTo(expected));
        }

        [Test]
        public void Can_ExecAs_ModelWithIdAndName_Func_List()
        {
            var expected = new[] {
                   ModelWithIdAndName.Create(1),
                   ModelWithIdAndName.Create(2),
                   ModelWithIdAndName.Create(3),
               };
            List<ModelWithIdAndName> actual = redisManager.ExecAs<ModelWithIdAndName>(m =>
            {
                var list = m.Lists["typed-list"];
                list.AddRange(expected);
                return list.GetAll();
            });
            Assert.That(actual.EquivalentTo(expected));
        }
    }
}