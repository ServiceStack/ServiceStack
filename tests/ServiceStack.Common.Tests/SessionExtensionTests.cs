#if !NETCORE_SUPPORT
using System;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class SessionExtensionTests
    {
        [Test]
        public void Does_CreateRandomSessionId_without_url_unfriendly_chars()
        {
            1000.Times(i =>
            {
                var sessionId = SessionExtensions.CreateRandomSessionId();
                Assert.That(sessionId, Is.Not.StringContaining("+"));
                Assert.That(sessionId, Is.Not.StringContaining("/"));
            });
        }

        [Test]
        public void ToBase64UrlSafe_does_not_contain_unfriendly_chars()
        {
            var bytes = new byte[24];
            string lastSessionId = null;

            1000.Times(i =>
            {
                SessionExtensions.PopulateWithSecureRandomBytes(bytes);

                var sessionId = bytes.ToBase64UrlSafe();

                Assert.That(sessionId, Is.Not.StringContaining("+"));
                Assert.That(sessionId, Is.Not.StringContaining("/"));

                if (lastSessionId != null)
                    Assert.That(sessionId, Is.Not.EqualTo(lastSessionId));
                lastSessionId = sessionId;
            });
        }

        [Test]
        public void Does_CreateRandomBase62Id_16_byte_id_in_less_than_3_attempts_avg()
        {
            Assert.That(SessionExtensions.CreateRandomBase62Id(16).Length, Is.EqualTo(24));

            int attempts = 0;
            1000.Times(i =>
            {
                do
                {
                    attempts++;
                } while (SessionExtensions.CreateRandomBase64Id(16).IndexOfAny(new[] {'+', '/'}) >= 0);
            });

            attempts.Print();
            Assert.That(attempts, Is.LessThan(1000 * 3));
        }

        [Test]
        public void CreateRandomBase64Id_contains_url_unfriendly_chars()
        {
            Assert.Throws<ArgumentException>(() =>
                1000.Times(i =>
                {
                    var sessionId = SessionExtensions.CreateRandomBase64Id();
                    if (sessionId.ContainsAny("+", "-"))
                        throw new ArgumentException("Url Unfriendly Chars found");
                }));
        }
    }
}
#endif