using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class SystemTimeTests
    {
        [Test]
        public void When_set_SystemTimeResolver_Then_should_get_correct_SystemTime_UtcNow()
        {
            var dateTime = new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            SystemTime.UtcDateTimeResolver = () => dateTime;
            Assert.AreEqual(dateTime.ToUniversalTime(), SystemTime.UtcNow);
        }

        [Test]
        public void When_set_UtcDateTimeResolver_Then_should_get_correct_SystemTime_Now()
        {
            var dateTime = new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Local);
            SystemTime.UtcDateTimeResolver = () => dateTime;
            Assert.AreEqual(dateTime, SystemTime.Now);
        }

        [Test]
        public void When_set_UtcDateTimeResolver_to_null_and_Then_should_get_correct_SystemTime_Now()
        {
            SystemTime.UtcDateTimeResolver = null;
            Assert.True(DateTime.UtcNow.IsEqualToTheSecond(SystemTime.UtcNow));
        }

        [Test]
        public void When_set_UtcDateTimeResolver_to_null_Then_should_get_correct_SystemTime_Now()
        {
            SystemTime.UtcDateTimeResolver = null;
            Assert.True(DateTime.Now.IsEqualToTheSecond(SystemTime.Now));
        }

        [TearDown]
        public void TearDown()
        {
            SystemTime.UtcDateTimeResolver = null;
        }
    }
}
