using System;
using NUnit.Framework;

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