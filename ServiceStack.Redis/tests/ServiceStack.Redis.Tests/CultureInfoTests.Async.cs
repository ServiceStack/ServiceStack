using NUnit.Framework;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class CultureInfoTestsAsync
        : RedisClientTestsBaseAsync
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
        public async Task Can_AddItemToSortedSet_in_different_Culture()
        {
            await RedisAsync.AddItemToSortedSetAsync("somekey1", "somevalue", 66121.202);
            var score = await RedisAsync.GetItemScoreInSortedSetAsync("somekey1", "somevalue");

            Assert.That(score, Is.EqualTo(66121.202));
        }

    }
}