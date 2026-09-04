#if NET6_0_OR_GREATER
using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Skia;
using SkiaSharp;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class SkiaImageProviderUnitTests
    {
        private static MemoryStream CreateTestPngStream(int width = 100, int height = 100)
        {
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Blue);
                using var paint = new SKPaint { Color = SKColors.Red };
                canvas.DrawRect(new SKRect(10, 10, width - 10, height - 10), paint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var ms = new MemoryStream();
            data.SaveTo(ms);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public void Resize_Creates_Valid_Png_Stream()
        {
            var provider = new SkiaImageProvider();
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

            using var decoded = SKBitmap.Decode(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(50));
            Assert.That(decoded.Height, Is.EqualTo(50));
        }

        [Test]
        public void Resize_With_Different_Aspect_Ratio_Resizes_And_Crops_Centered()
        {
            var provider = new SkiaImageProvider();
            using var input = CreateTestPngStream(200, 100);

            using var output = provider.Resize(input, 50, 50);

            using var decoded = SKBitmap.Decode(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(50));
            Assert.That(decoded.Height, Is.EqualTo(50));
        }

        [Test]
        public void Resize_Upscaling_Works_Correctly()
        {
            var provider = new SkiaImageProvider();
            using var input = CreateTestPngStream(20, 20);

            using var output = provider.Resize(input, 80, 80);

            using var decoded = SKBitmap.Decode(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(80));
            Assert.That(decoded.Height, Is.EqualTo(80));
        }

        [Test]
        public void Resize_When_Dimensions_Match_Encodes_To_Png()
        {
            var provider = new SkiaImageProvider();
            using var input = CreateTestPngStream(64, 64);

            using var output = provider.Resize(input, 64, 64);

            using var decoded = SKBitmap.Decode(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(64));
            Assert.That(decoded.Height, Is.EqualTo(64));
        }

        [Test]
        public void Resize_Rewinds_Stream_If_CanSeek_And_Position_Not_Zero()
        {
            var provider = new SkiaImageProvider();
            using var input = CreateTestPngStream(100, 100);
            input.Position = input.Length; // Position at end

            using var output = provider.Resize(input, 30, 30);

            using var decoded = SKBitmap.Decode(output);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Width, Is.EqualTo(30));
            Assert.That(decoded.Height, Is.EqualTo(30));
        }

        [Test]
        public void Crop_Does_Not_Dispose_Input_Bitmap()
        {
            using var bitmap = new SKBitmap(100, 100);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Green);
            }

            using var cropped = SkiaImageProvider.Crop(bitmap, 50, 50);

            Assert.That(cropped.Width, Is.EqualTo(50));
            Assert.That(cropped.Height, Is.EqualTo(50));

            // Verify original bitmap is NOT disposed and remains completely usable
            Assert.DoesNotThrow(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(100));
                Assert.That(bitmap.Height, Is.EqualTo(100));
                var pixel = bitmap.GetPixel(10, 10);
                Assert.That(pixel, Is.EqualTo(SKColors.Green));
            });
        }

        [Test]
        public void Resize_Throws_On_Null_Stream()
        {
            var provider = new SkiaImageProvider();
            Assert.Throws<ArgumentNullException>(() =>
            {
                provider.Resize(null, 50, 50);
            });
        }

        [Test]
        public void Resize_Throws_On_Invalid_Dimensions()
        {
            var provider = new SkiaImageProvider();
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
        public void Resize_Throws_On_Corrupt_Or_Empty_Stream()
        {
            var provider = new SkiaImageProvider();

            using var emptyStream = new MemoryStream();
            var ex1 = Assert.Throws<ArgumentException>(() =>
            {
                provider.Resize(emptyStream, 50, 50);
            });
            Assert.That(ex1.Message, Does.Contain("Failed to decode image from stream"));

            using var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            var ex2 = Assert.Throws<ArgumentException>(() =>
            {
                provider.Resize(invalidStream, 50, 50);
            });
            Assert.That(ex2.Message, Does.Contain("Failed to decode image from stream"));
        }

        [Test]
        public void Crop_Throws_On_Null_Bitmap()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SkiaImageProvider.Crop(null, 50, 50);
            });
        }

        [Test]
        public void Crop_Throws_On_Invalid_Dimensions()
        {
            using var bitmap = new SKBitmap(10, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SkiaImageProvider.Crop(bitmap, -5, 10);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SkiaImageProvider.Crop(bitmap, 10, 0);
            });
        }

        [Test]
        public void SkiaImageExtensions_Work_As_Expected()
        {
            using var bitmap = new SKBitmap(80, 80);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Yellow);
            }

            using var stream = bitmap.ResizeToPng(40, 40);
            Assert.That(stream, Is.Not.Null);

            using var decoded = SKBitmap.Decode(stream);
            Assert.That(decoded.Width, Is.EqualTo(40));
            Assert.That(decoded.Height, Is.EqualTo(40));

            using var cropped = bitmap.Crop(30, 30);
            Assert.That(cropped.Width, Is.EqualTo(30));
            Assert.That(cropped.Height, Is.EqualTo(30));
        }
    }
}
#endif
