using System.IO;

namespace ServiceStack.Host;

public interface IHasBufferedStream
{
    MemoryStream BufferedStream { get; }
}
