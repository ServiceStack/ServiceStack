using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    [TestFixture]
    public class CustomCommandTests
        : RedisClientTestsBase
    {
        [Test]
        public void Can_send_custom_commands()
        {
            Redis.FlushAll();

            RedisText ret;

            ret = Redis.Custom("SET", "foo", 1);
            Assert.That(ret.Text, Is.EqualTo("OK"));
            ret = Redis.Custom(Commands.Set, "bar", "b");

            ret = Redis.Custom("GET", "foo");
            Assert.That(ret.Text, Is.EqualTo("1"));
            ret = Redis.Custom(Commands.Get, "bar");
            Assert.That(ret.Text, Is.EqualTo("b"));

            ret = Redis.Custom(Commands.Keys, "*");
            var keys = ret.GetResults();
            Assert.That(keys, Is.EquivalentTo(new[] { "foo", "bar" }));

            ret = Redis.Custom("MGET", "foo", "bar");
            var values = ret.GetResults();
            Assert.That(values, Is.EquivalentTo(new[] { "1", "b" }));

            Enum.GetNames(typeof(DayOfWeek)).ToList()
                .ForEach(x => Redis.Custom("RPUSH", "DaysOfWeek", x));

            ret = Redis.Custom("LRANGE", "DaysOfWeek", 1, -2);

            var weekDays = ret.GetResults();
            Assert.That(weekDays, Is.EquivalentTo(
                new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }));

            ret.PrintDump();
        }

        [Test]
        public void Can_send_complex_types_in_Custom_Commands()
        {
            Redis.FlushAll();

            RedisText ret;

            ret = Redis.Custom("SET", "foo", new Poco { Name = "Bar" });
            Assert.That(ret.Text, Is.EqualTo("OK"));

            ret = Redis.Custom("GET", "foo");
            var dto = ret.GetResult<Poco>();
            Assert.That(dto.Name, Is.EqualTo("Bar"));

            Enum.GetNames(typeof(DayOfWeek)).ToList()
                .ForEach(x => Redis.Custom("RPUSH", "DaysOfWeek", new Poco { Name = x }));

            ret = Redis.Custom("LRANGE", "DaysOfWeek", 1, -2);
            var weekDays = ret.GetResults<Poco>();

            Assert.That(weekDays.First().Name, Is.EqualTo("Monday"));

            ret.PrintDump();
        }
    }
}