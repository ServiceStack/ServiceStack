using System;
using System.IO;
using ServiceStack.Text;
using SkiaSharp;

namespace ServiceStack.Skia;

public class SkiaImageProvider : ImageProvider
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

        using var img = SKBitmap.Decode(stream);
        if (img == null)
            throw new ArgumentException("Failed to decode image from stream.", nameof(stream));

        return ResizeToPng(img, newWidth, newHeight);
    }
    
    public Stream ResizeToPng(SKBitmap img, int newWidth, int newHeight) =>
        ResizeToPng(img, newWidth, newHeight, 75);

    public Stream ResizeToPng(SKBitmap img, int newWidth, int newHeight, int quality)
    {
        if (img == null)
            throw new ArgumentNullException(nameof(img));
        if (newWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(newWidth), "Width must be greater than zero.");
        if (newHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(newHeight), "Height must be greater than zero.");
        if (img.Width <= 0 || img.Height <= 0)
            throw new ArgumentException("Image must have valid dimensions greater than zero.", nameof(img));

        SKBitmap current = img;
        bool isIntermediate = false;

        try
        {
            if (newWidth != current.Width || newHeight != current.Height)
            {
                var ratioX = (double)newWidth / current.Width;
                var ratioY = (double)newHeight / current.Height;
                var ratio = Math.Max(ratioX, ratioY);
                var width = Math.Max(1, (int)Math.Round(current.Width * ratio));
                var height = Math.Max(1, (int)Math.Round(current.Height * ratio));

                var resized = current.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
                if (resized == null)
                    throw new InvalidOperationException("Failed to resize image.");

                current = resized;
                isIntermediate = true;

                if (current.Width != newWidth || current.Height != newHeight)
                {
                    var cropped = Crop(current, newWidth, newHeight);
                    current.Dispose();
                    current = cropped;
                }
            }

            using var image = SKImage.FromBitmap(current);
            using var data = image.Encode(SKEncodedImageFormat.Png, quality);
            if (data == null)
                throw new InvalidOperationException("Failed to encode image to PNG.");

            var ms = MemoryStreamFactory.GetStream();
            data.SaveTo(ms);
            ms.Position = 0;
            return ms;
        }
        finally
        {
            if (isIntermediate)
            {
                current.Dispose();
            }
        }
    }    
    
    public static SKBitmap Crop(SKBitmap img, int newWidth, int newHeight)
    {
        if (img == null)
            throw new ArgumentNullException(nameof(img));
        if (newWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(newWidth), "Width must be greater than zero.");
        if (newHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(newHeight), "Height must be greater than zero.");
        if (img.Width <= 0 || img.Height <= 0)
            throw new ArgumentException("Image must have valid dimensions greater than zero.", nameof(img));

        if (img.Width < newWidth)
            newWidth = img.Width;

        if (img.Height < newHeight)
            newHeight = img.Height;

        var startX = (img.Width - newWidth) / 2;
        var startY = (img.Height - newHeight) / 2;

        var croppedBitmap = new SKBitmap(newWidth, newHeight);
        var source = new SKRect(startX, startY, newWidth + startX, newHeight + startY);
        var dest = new SKRect(0, 0, newWidth, newHeight);
        using var canvas = new SKCanvas(croppedBitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(img, source, dest);
            
        return croppedBitmap;
    }    
}

public static class SkiaImageExtensions
{
    public static Stream ResizeToPng(this SKBitmap img, int newWidth, int newHeight, int quality = 75) =>
        new SkiaImageProvider().ResizeToPng(img, newWidth, newHeight, quality);

    public static SKBitmap Crop(this SKBitmap img, int newWidth, int newHeight) =>
        SkiaImageProvider.Crop(img, newWidth, newHeight);
}