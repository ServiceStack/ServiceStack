#nullable enable

using System.IO;
using System.IO.Compression;
using System.Text;

namespace ServiceStack.Caching;

/// <summary>
/// Implement Stream Compressor, e.g. br, deflate, gzip
/// </summary>
public interface IStreamCompressor
{
    string Encoding { get; }
    
    byte[] Compress(string text, Encoding? encoding = null);
    byte[] Compress(byte[] bytes);
    Stream Compress(Stream outputStream, bool leaveOpen=false);

    string Decompress(byte[] zipBuffer, Encoding? encoding = null);
        
    Stream Decompress(Stream zipBuffer, bool leaveOpen=false);

    byte[] DecompressBytes(byte[] zipBuffer);
}