using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        [TestCase]
        public void LastMondayTest()
        {
            var monday = new DateTime(2013, 04, 15);

            var lastMonday = DateTimeExtensions.LastMonday(monday);

            Assert.AreEqual(monday, lastMonday);
        } 
    }
}