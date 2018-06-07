using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ZipTests
    {
        private readonly bool hold;
        public ZipTests()
        {
            hold = MemoryStreamFactory.UseRecyclableMemoryStream;
            MemoryStreamFactory.UseRecyclableMemoryStream = true;
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown() => MemoryStreamFactory.UseRecyclableMemoryStream = hold;

        [Test]
        public void Can_zip_and_unzip_bytes_using_DeflateStream()
        {
            var text = "hello zip";
            var zipBytes = StreamExt.DeflateProvider.Deflate(text);
            var unzip = StreamExt.DeflateProvider.Inflate(zipBytes);
            Assert.That(unzip, Is.EqualTo(text));
        }

        [Test]
        public void Can_zip_and_unzip_bytes_using_Gzip()
        {
            var text = "hello zip";
            var zipBytes = StreamExt.GZipProvider.GZip(text);
            var unzip = StreamExt.GZipProvider.GUnzip(zipBytes);
            Assert.That(unzip, Is.EqualTo(text));
        }
    }
}