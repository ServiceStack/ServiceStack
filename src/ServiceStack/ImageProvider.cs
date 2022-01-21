#nullable  enable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using ServiceStack.Text;

namespace ServiceStack;

public abstract class ImageProvider
{
    public static ImageProvider Instance { get; set; } = new ImageDrawingProvider();

    public abstract Stream Resize(Stream stream, int newWidth, int newHeight);

    public virtual Stream Resize(Stream origStream, string? savePhotoSize = null)
    {
        var parts = savePhotoSize?.Split('x');
        if (parts is { Length: > 1 } &&
            int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
            return Resize(origStream, width, height);

        return origStream;
    }
}

public class ImageDrawingProvider : ImageProvider
{
    public override Stream Resize(Stream origStream, int newWidth, int newHeight)
    {
        using var origImage = Image.FromStream(origStream);
        return origImage.ResizeToPng(newWidth, newHeight);
    }
}

public static class ImageExtensions
{
    public static MemoryStream ResizeToPng(this Image img, int newWidth, int newHeight)
    {
        if (newWidth != img.Width || newHeight != img.Height)
        {
            var ratioX = (double)newWidth / img.Width;
            var ratioY = (double)newHeight / img.Height;
            var ratio = Math.Max(ratioX, ratioY);
            var width = (int)(img.Width * ratio);
            var height = (int)(img.Height * ratio);

            using var newImage = new Bitmap(width, height);
            Graphics.FromImage(newImage).DrawImage(img, 0, 0, width, height);

            if (newImage.Width != newWidth || newImage.Height != newHeight)
            {
                var startX = (Math.Max(newImage.Width, newWidth) - Math.Min(newImage.Width, newWidth)) / 2;
                var startY = (Math.Max(newImage.Height, newHeight) - Math.Min(newImage.Height, newHeight)) / 2;
                return CropToPng(img, newWidth, newHeight, startX, startY);
            }

            var ms = MemoryStreamFactory.GetStream();
            newImage.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }
        else
        {
            var ms = MemoryStreamFactory.GetStream();
            img.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }
    }

    public static MemoryStream CropToPng(this Image img, int newWidth, int newHeight, int startX = 0, int startY = 0)
    {
        if (img.Height < newHeight)
            newHeight = img.Height;

        if (img.Width < newWidth)
            newWidth = img.Width;

        using var bmp = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
        bmp.SetResolution(72, 72);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(img, new Rectangle(0, 0, newWidth, newHeight), startX, startY, newWidth, newHeight,
            GraphicsUnit.Pixel);

        var ms = MemoryStreamFactory.GetStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        return ms;
    }    
}