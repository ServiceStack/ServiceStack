using System.IO;
using System.Text;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.ActiveMq.Support.Utils;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests.Utils
{
    [TestFixture]
    public class SharpZipUtilsTests : UnitTestCaseBase
    {
        private static string testXml = null;

        public static string TestXml
        {
            get
            {
                if (testXml == null)
                {
                    XmlSerializableObject xmlObj = new XmlSerializableObject();
                    testXml = new XmlSerializableSerializer().Parse(xmlObj);
                }
                return testXml;
            }
        }

        [Test]
        public void ZipXmlSerializableObjectTest()
        {
            byte[] originalUncompressedBuffer = new UTF8Encoding().GetBytes(TestXml);
            byte[] compressedBuffer = SharpZipUtils.Compress(originalUncompressedBuffer);

            Assert.IsTrue(compressedBuffer.Length > 0);
            byte[] uncompressedBuffer = SharpZipUtils.Decompress(compressedBuffer);

            Assert.AreEqual(originalUncompressedBuffer.Length, uncompressedBuffer.Length, "Uncompressed lengths should be the same");
        }

        [Test]
        public void ZipLargeXmlTest()
        {
            byte[] originalUncompressedBuffer = File.ReadAllBytes(LargeXmlPath);

            byte[] compressedBuffer = SharpZipUtils.Compress(originalUncompressedBuffer);
            Assert.IsTrue(compressedBuffer.Length > 0);
            byte[] uncompressedBuffer = SharpZipUtils.Decompress(compressedBuffer);

            Assert.AreEqual(originalUncompressedBuffer.Length, uncompressedBuffer.Length, "Uncompressed lengths should be the same");
        }
    }
}