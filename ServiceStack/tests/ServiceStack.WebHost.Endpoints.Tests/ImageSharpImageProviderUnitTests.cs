#if IMAGE_SHARP
using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ImageSharpImageProviderUnitTests
    {
        private static MemoryStream CreateTestPngStream(int width = 100, int height = 100)
        {
            using var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx => ctx.BackgroundColor(Color.Blue));

            var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public void Resize_Creates_Valid_Png_Stream()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(100, 100);

            using var output = provider.Resize(input, 50, 50);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.CanSeek, Is.True);
            Assert.That(output.Position, Is.EqualTo(0));
            Assert.That(output.Length, Is.GreaterThan(0));

            // Verify PNG magic header: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
            var header = new byte[8];
            output.ReadExactly(header, 0, 8);
            output.Position = 0;

            Assert.That(header, Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));

            using var decoded = Image.Load(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(50));
            Assert.That(decoded.Height, Is.EqualTo(50));
        }

        [Test]
        public void Resize_Does_Not_Dispose_Input_Stream()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(100, 100);

            using var output = provider.Resize(input, 50, 50);

            // Verify input stream remains open and accessible
            Assert.DoesNotThrow(() =>
            {
                Assert.That(input.CanRead, Is.True);
                Assert.That(input.CanSeek, Is.True);
                input.Position = 0;
                var b = input.ReadByte();
                Assert.That(b, Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void Resize_With_Different_Aspect_Ratio_Resizes_And_Crops_Centered()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(200, 100);

            using var output = provider.Resize(input, 50, 50);

            using var decoded = Image.Load(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(50));
            Assert.That(decoded.Height, Is.EqualTo(50));
        }

        [Test]
        public void Resize_Upscaling_Works_Correctly()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(20, 20);

            using var output = provider.Resize(input, 80, 80);

            using var decoded = Image.Load(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(80));
            Assert.That(decoded.Height, Is.EqualTo(80));
        }

        [Test]
        public void Resize_When_Dimensions_Match_Encodes_To_Png()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(64, 64);

            using var output = provider.Resize(input, 64, 64);

            using var decoded = Image.Load(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(64));
            Assert.That(decoded.Height, Is.EqualTo(64));
        }

        [Test]
        public void Resize_Rewinds_Stream_If_CanSeek_And_Position_Not_Zero()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(100, 100);
            input.Position = input.Length; // Position at end

            using var output = provider.Resize(input, 30, 30);

            using var decoded = Image.Load(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(30));
            Assert.That(decoded.Height, Is.EqualTo(30));
        }

        [Test]
        public void Resize_Throws_On_Null_Stream()
        {
            var provider = new ImageSharpImageProvider();
            Assert.Throws<ArgumentNullException>(() =>
            {
                provider.Resize(null, 50, 50);
            });
        }

        [Test]
        public void Resize_Throws_On_Invalid_Dimensions()
        {
            var provider = new ImageSharpImageProvider();
            using var input = CreateTestPngStream(10, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                provider.Resize(input, 0, 50);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                provider.Resize(input, 50, -1);
            });
        }

        [Test]
        public void ImageSharpExtensions_Work_As_Expected()
        {
            using var image = new Image<Rgba32>(80, 80);
            image.Mutate(ctx => ctx.BackgroundColor(Color.Yellow));

            using var stream = image.ResizeToPng(40, 40);
            Assert.That(stream, Is.Not.Null);

            using var decoded = Image.Load(stream);
            Assert.That(decoded.Width, Is.EqualTo(40));
            Assert.That(decoded.Height, Is.EqualTo(40));
        }
    }
}
#endif
