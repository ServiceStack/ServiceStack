#if !NETCORE_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class CryptUtilsTest
    {
        [TestCase]
        public void CanEncryptWithStringExtension()
        {
            RsaUtils.KeyLength = RsaKeyLengths.Bit1024;
            RsaUtils.DefaultKeyPair = RsaUtils.CreatePublicAndPrivateKeyPair();

            string TestStart = "Mr. Watson--come here--I want to see you.";
            string Encrypted;
            string Decrypted;

            Encrypted = TestStart.Encrypt();
            Assert.AreNotEqual(Encrypted, TestStart);

            Decrypted = Encrypted.Decrypt();
            Assert.AreEqual(Decrypted, TestStart);

        }

        [Test]
        public void Can_sign_data_with_RSA()
        {
            var privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);
            var publicKey = privateKey.ToPublicRsaParameters();

            var message = "sign this";
            var data = message.ToUtf8Bytes();

            var signature = RsaUtils.Authenticate(data, privateKey, "SHA256", RsaKeyLengths.Bit2048);

            var verified = RsaUtils.Verify(data, signature, publicKey, "SHA256", RsaKeyLengths.Bit2048);

            Assert.That(verified, Is.True);
        }
    }
}
#endif