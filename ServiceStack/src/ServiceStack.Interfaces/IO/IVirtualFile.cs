using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.IO;

public interface IVirtualFile : IVirtualNode
{
    IVirtualPathProvider VirtualPathProvider { get; }

    /// <summary>
    /// The file extension without '.' prefix
    /// </summary>
    string Extension { get; }

    string GetFileHash();

    Stream OpenRead();
    StreamReader OpenText();
    string ReadAllText();

    /// <summary>
    /// Returns ReadOnlyMemory&lt;byte&gt; for binary files or
    /// ReadOnlyMemory&lt;char&gt; for text files   
    /// </summary>
    object GetContents();

    long Length { get; }

    /// <summary>
    /// Refresh file stats for this node if supported
    /// </summary>
    void Refresh();

    Task WritePartialToAsync(Stream toStream, long start, long end, CancellationToken token = default);
}