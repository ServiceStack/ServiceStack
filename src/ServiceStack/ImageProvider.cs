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

    public abstract MemoryStream Resize(Stream origStream, string savePhotoSize = null);
    
    public abstract MemoryStream ResizeToPng(Image img, int newWidth, int newHeight);

    public abstract MemoryStream CropToPng(Image img, int newWidth, int newHeight, int startX = 0, int startY = 0);
}

public class ImageDrawingProvider : ImageProvider
{
    public override MemoryStream Resize(Stream origStream, string savePhotoSize = null)
    {
        using var origImage = Image.FromStream(origStream);
        var parts = savePhotoSize?.Split('x');
        var width = origImage.Width;
        var height = origImage.Height;

        if (parts is { Length: > 0 })
            int.TryParse(parts[0], out width);

        if (parts is { Length: > 1 })
            int.TryParse(parts[1], out height);

        return origImage.ResizeToPng(width, height);
    }

    public override MemoryStream ResizeToPng(Image img, int newWidth, int newHeight)
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
    
    public override MemoryStream CropToPng(Image img, int newWidth, int newHeight, int startX = 0, int startY = 0)
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

public static class ImageExtensions
{
    public static MemoryStream ResizeToPng(this Image img, int newWidth, int newHeight) =>
        ImageProvider.Instance.ResizeToPng(img, newWidth, newHeight);

    public static MemoryStream CropToPng(this Image img, int newWidth, int newHeight, int startX = 0, int startY = 0) =>
        ImageProvider.Instance.CropToPng(img, newWidth, newHeight, startX, startY);
}