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
        [TestFixtureSetUp]
        public void Setup()
        {

            CryptConfig.length = RsaKeyLengths.Bit1024;
            CryptConfig.keyPair = CryptConfig.CreatePublicAndPrivateKeyPair();
            //RsaKeyLengths.Bit1024;
        }


        [TestCase]
        public void CanEncryptWithStringExtension()
        {
            string TestStart = "Mr. Watson--come here--I want to see you.";
            string Encrypted;
            string Decrypted;

            Encrypted = TestStart.Encrypt();
            Assert.AreNotEqual(Encrypted, TestStart);

            Decrypted = Encrypted.Decrypt();
            Assert.AreEqual(Decrypted, TestStart);

        }
    }
}