using System;
using System.IO;
using ServiceStack.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ServiceStack.ImageSharp;

public class ImageSharpImageProvider : ImageProvider
{
    public override Stream Resize(Stream stream, int newWidth, int newHeight)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (newWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(newWidth), "Width must be greater than zero.");
        if (newHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(newHeight), "Height must be greater than zero.");

        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        using var image = Image.Load(stream);

        var options = new ResizeOptions
        {
            Size = new Size(newWidth, newHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        };
        image.Mutate(i => i.Resize(options));

        var outputStream = MemoryStreamFactory.GetStream();
        image.SaveAsPng(outputStream);
        outputStream.Position = 0;
        return outputStream;
    }
}

public static class ImageSharpExtensions
{
    public static Stream ResizeToPng(this Image image, int newWidth, int newHeight)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        if (newWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(newWidth), "Width must be greater than zero.");
        if (newHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(newHeight), "Height must be greater than zero.");

        var options = new ResizeOptions
        {
            Size = new Size(newWidth, newHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        };
        image.Mutate(i => i.Resize(options));

        var outputStream = MemoryStreamFactory.GetStream();
        image.SaveAsPng(outputStream);
        outputStream.Position = 0;
        return outputStream;
    }
}