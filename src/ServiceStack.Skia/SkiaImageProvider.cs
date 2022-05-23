using SkiaSharp;

namespace ServiceStack.Skia;

public class SkiaImageProvider : ImageProvider
{
    public override Stream Resize(Stream stream, int newWidth, int newHeight)
    {
        using var img = SKBitmap.Decode(stream);
        return ResizeToPng(img, newWidth, newHeight);
    }
    
    public Stream ResizeToPng(SKBitmap img, int newWidth, int newHeight)
    {
        if (newWidth != img.Width || newHeight != img.Height)
        {
            var ratioX = (double)newWidth / img.Width;
            var ratioY = (double)newHeight / img.Height;
            var ratio = Math.Max(ratioX, ratioY);
            var width = (int)(img.Width * ratio);
            var height = (int)(img.Height * ratio);

            img = img.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
            if (img.Width != newWidth || img.Height != newHeight)
            {
                img = Crop(img, newWidth, newHeight); // resized + clipped
            }
        }

        var pngStream = img.Encode(SKEncodedImageFormat.Png, 75).AsStream();
        return pngStream;
    }    
    
    public static SKBitmap Crop(SKBitmap img, int newWidth, int newHeight)
    {
        if (img.Width < newWidth)
            newWidth = img.Width;

        if (img.Height < newHeight)
            newHeight = img.Height;

        var startX = (Math.Max(img.Width, newWidth) - Math.Min(img.Width, newWidth)) / 2;
        var startY = (Math.Max(img.Height, newHeight) - Math.Min(img.Height, newHeight)) / 2;

        var croppedBitmap = new SKBitmap(newWidth, newHeight);
        var source = new SKRect(startX, startY, newWidth + startX, newHeight + startY);
        var dest = new SKRect(0, 0, newWidth, newHeight);
        using var canvas = new SKCanvas(croppedBitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(img, source, dest);
        img.Dispose();
            
        return croppedBitmap;
    }    
}