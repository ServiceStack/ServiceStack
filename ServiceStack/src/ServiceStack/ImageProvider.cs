#nullable  enable

using System;
using System.IO;

namespace ServiceStack;

public abstract class ImageProvider
{
    public static ImageProvider Instance { get; set; } = new MissingImageDrawingProvider();

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

public class MissingImageDrawingProvider : ImageProvider
{
    public override Stream Resize(Stream origStream, int newWidth, int newHeight)
    {
        throw new NotImplementedException("ImageProvider.Instance not set. Can use System.Drawing ImageDrawingProvider in ServiceStack.Desktop or " +
            "ServiceStack.ImageSharp or ServiceStack.Skia implementations in https://github.com/ServiceStack/ServiceStack/tree/main/ServiceStack/src/");
    }
}
