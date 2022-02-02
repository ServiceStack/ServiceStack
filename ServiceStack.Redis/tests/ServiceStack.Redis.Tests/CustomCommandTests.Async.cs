using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class CustomCommandTestsAsync
        : RedisClientTestsBaseAsync
    {
        [Test]
        public async Task Can_send_custom_commands()
        {
            await RedisAsync.FlushAllAsync();

            RedisText ret;

            ret = await RedisAsync.CustomAsync("SET", "foo", 1);
            Assert.That(ret.Text, Is.EqualTo("OK"));
            _ = await RedisAsync.CustomAsync(Commands.Set, "bar", "b");

            ret = await RedisAsync.CustomAsync("GET", "foo");
            Assert.That(ret.Text, Is.EqualTo("1"));
            ret = await RedisAsync.CustomAsync(Commands.Get, "bar");
            Assert.That(ret.Text, Is.EqualTo("b"));

            ret = await RedisAsync.CustomAsync(Commands.Keys, "*");
            var keys = ret.GetResults();
            Assert.That(keys, Is.EquivalentTo(new[] { "foo", "bar" }));

            ret = await RedisAsync.CustomAsync("MGET", "foo", "bar");
            var values = ret.GetResults();
            Assert.That(values, Is.EquivalentTo(new[] { "1", "b" }));

            foreach (var x in Enum.GetNames(typeof(DayOfWeek)))
            {
                await RedisAsync.CustomAsync("RPUSH", "DaysOfWeek", x);
            }

            ret = await RedisAsync.CustomAsync("LRANGE", "DaysOfWeek", 1, -2);

            var weekDays = ret.GetResults();
            Assert.That(weekDays, Is.EquivalentTo(
                    new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }));

            ret.PrintDump();
        }

        [Test]
        public async Task Can_send_custom_commands_longhand()
        {
            await RedisAsync.FlushAllAsync();

            RedisText ret;

            ret = await RedisAsync.CustomAsync(new object[] { "SET", "foo", 1 });
            Assert.That(ret.Text, Is.EqualTo("OK"));
            _ = await RedisAsync.CustomAsync(new object[] { Commands.Set, "bar", "b" });

            ret = await RedisAsync.CustomAsync(new object[] { "GET", "foo" });
            Assert.That(ret.Text, Is.EqualTo("1"));
            ret = await RedisAsync.CustomAsync(new object[] { Commands.Get, "bar" });
            Assert.That(ret.Text, Is.EqualTo("b"));

            ret = await RedisAsync.CustomAsync(new object[] { Commands.Keys, "*" });
            var keys = ret.GetResults();
            Assert.That(keys, Is.EquivalentTo(new[] { "foo", "bar" }));

            ret = await RedisAsync.CustomAsync(new object[] { "MGET", "foo", "bar" });
            var values = ret.GetResults();
            Assert.That(values, Is.EquivalentTo(new[] { "1", "b" }));

            foreach (var x in Enum.GetNames(typeof(DayOfWeek)))
            {
                await RedisAsync.CustomAsync(new object[] { "RPUSH", "DaysOfWeek", x });
            }

            ret = await RedisAsync.CustomAsync(new object[] { "LRANGE", "DaysOfWeek", 1, -2 });

            var weekDays = ret.GetResults();
            Assert.That(weekDays, Is.EquivalentTo(
                    new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }));

            ret.PrintDump();
        }

        [Test]
        public async Task Can_send_complex_types_in_Custom_Commands()
        {
            await RedisAsync.FlushAllAsync();

            RedisText ret;

            ret = await RedisAsync.CustomAsync("SET", "foo", new Poco { Name = "Bar" });
            Assert.That(ret.Text, Is.EqualTo("OK"));

            ret = await RedisAsync.CustomAsync("GET", "foo");
            var dto = ret.GetResult<Poco>();
            Assert.That(dto.Name, Is.EqualTo("Bar"));

            foreach (var x in Enum.GetNames(typeof(DayOfWeek)))
                await RedisAsync.CustomAsync("RPUSH", "DaysOfWeek", new Poco { Name = x });

            ret = await RedisAsync.CustomAsync("LRANGE", "DaysOfWeek", 1, -2);
            var weekDays = ret.GetResults<Poco>();

            Assert.That(weekDays.First().Name, Is.EqualTo("Monday"));

            ret.PrintDump();
        }

        [Test]
        public async Task Can_send_complex_types_in_Custom_Commands_longhand()
        {
            await RedisAsync.FlushAllAsync();

            RedisText ret;

            ret = await RedisAsync.CustomAsync(new object[] { "SET", "foo", new Poco { Name = "Bar" } });
            Assert.That(ret.Text, Is.EqualTo("OK"));

            ret = await RedisAsync.CustomAsync(new object[] { "GET", "foo" });
            var dto = ret.GetResult<Poco>();
            Assert.That(dto.Name, Is.EqualTo("Bar"));

            foreach (var x in Enum.GetNames(typeof(DayOfWeek)))
                await RedisAsync.CustomAsync(new object[] { "RPUSH", "DaysOfWeek", new Poco { Name = x } });

            ret = await RedisAsync.CustomAsync(new object[] { "LRANGE", "DaysOfWeek", 1, -2 });
            var weekDays = ret.GetResults<Poco>();

            Assert.That(weekDays.First().Name, Is.EqualTo("Monday"));

            ret.PrintDump();
        }
    }
}