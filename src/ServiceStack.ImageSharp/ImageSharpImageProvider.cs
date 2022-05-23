using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ServiceStack.ImageSharp;

public class ImageSharpImageProvider : ImageProvider
{
    public override Stream Resize(Stream stream, int newWidth, int newHeight)
    {
        var outputStream = new MemoryStream();
        using var inputStream = stream;
        using var image = Image.Load(inputStream);
        image.Mutate(i => i.Resize(newWidth, newHeight));
        image.SaveAsPng(outputStream);

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }
}