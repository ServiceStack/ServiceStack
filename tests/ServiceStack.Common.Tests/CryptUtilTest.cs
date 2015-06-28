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
            Rsa.KeyLength = RsaKeyLengths.Bit1024;
            Rsa.DefaultKeyPair = Rsa.CreatePublicAndPrivateKeyPair();

            string TestStart = "Mr. Watson--come here--I want to see you.";
            string Encrypted;
            string Decrypted;

            Encrypted = TestStart.Encrypt();
            Assert.AreNotEqual(Encrypted, TestStart);

            Decrypted = Encrypted.Decrypt();
            Assert.AreEqual(Decrypted, TestStart);

        }

        [TestCase]
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void CanEncryptWithStringExtensionFailsWithoutKeyPair()
        {
            Rsa.KeyLength = RsaKeyLengths.Bit1024;
            Rsa.DefaultKeyPair = null;
            string TestStart = "Mr. Watson--come here--I want to see you.";
            string Encrypted;

            Encrypted = TestStart.Encrypt();
                      

        }
    }
}