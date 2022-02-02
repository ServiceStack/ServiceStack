using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisExtensionTests
    {
        [Test]
        public void Can_Parse_Host()
        {
            var hosts = new[] { "pass@host.com:6123" };
            var endPoints = hosts.ToRedisEndPoints();

            Assert.AreEqual(1, endPoints.Count);
            var ep = endPoints[0];

            Assert.AreEqual("host.com", ep.Host);
            Assert.AreEqual(6123, ep.Port);
            Assert.AreEqual("pass", ep.Password);
        }

        [Test]
        public void Host_May_Contain_AtChar()
        {
            var hosts = new[] { "@pa1@ss@localhost:6123" };
            var endPoints = hosts.ToRedisEndPoints();

            Assert.AreEqual(1, endPoints.Count);
            var ep = endPoints[0];

            Assert.AreEqual("@pa1@ss", ep.Password);
            Assert.AreEqual("localhost", ep.Host);
            Assert.AreEqual(6123, ep.Port);
        }
    }
}
