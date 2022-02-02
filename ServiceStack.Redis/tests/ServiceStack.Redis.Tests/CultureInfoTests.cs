using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class CultureInfoTests
        : RedisClientTestsBase
    {
        private CultureInfo previousCulture = CultureInfo.InvariantCulture;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
#if NETCORE
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
#else
            previousCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
#if NETCORE
            CultureInfo.CurrentCulture = previousCulture;
#else
            Thread.CurrentThread.CurrentCulture = previousCulture;
#endif
        }

        [Test]
        public void Can_AddItemToSortedSet_in_different_Culture()
        {
            Redis.AddItemToSortedSet("somekey1", "somevalue", 66121.202);
            var score = Redis.GetItemScoreInSortedSet("somekey1", "somevalue");

            Assert.That(score, Is.EqualTo(66121.202));
        }

    }
}