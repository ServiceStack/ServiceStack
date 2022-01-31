#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack;

public static class StreamCompressors
{
    static Dictionary<string, IStreamCompressor> Compressors { get; } = new()
    {
#if NET6_0_OR_GREATER
        { "br", BrotliCompressor.Instance }, //CompressionTypes.Brotli
        { CompressionTypes.Deflate, ZLibCompressor.Instance },
#else        
        { CompressionTypes.Deflate, DeflateCompressor.Instance },
#endif
        { CompressionTypes.GZip, GZipCompressor.Instance },
    };

    /// <summary>
    /// Register a new compressor for a specific encoding (defaults: gzip, deflate, br*) .NET6+ 
    /// </summary>
    public static void Set(string encoding, IStreamCompressor compressor) =>
        Compressors[encoding] = compressor;
    
    /// <summary>
    /// Is there a compressor registered with this encoding?
    /// </summary>
    public static bool SupportsEncoding(string? encoding) => encoding != null && Compressors.ContainsKey(encoding);

    /// <summary>
    /// return the registered IStreamCompressor implementation for for this  
    /// </summary>
    public static IStreamCompressor? Get(string? encoding) => encoding != null && Compressors.TryGetValue(encoding, out var compressor)
        ? compressor
        : null;
    
    /// <summary>
    /// Assert there exists a IStreamCompressor for this encoding
    /// </summary>
    public static IStreamCompressor GetRequired(string encoding) => Get(encoding)
        ?? throw new NotSupportedException($"{encoding} is not a registered IStreamCompressor, only: "
            + string.Join(", ", Compressors.Keys.ToString()));

    /// <summary>
    /// Remove compression support for this encoding
    /// </summary>
    public static bool Remove(string encoding) => Compressors.Remove(encoding);
}


#if NET6_0_OR_GREATER
public class BrotliCompressor : IStreamCompressor
{
    public string Encoding => "br";  //CompressionTypes.Brotli
    public static BrotliCompressor Instance { get; } = new();

    public byte[] Compress(string text, Encoding? encoding = null) => Compress((encoding ?? System.Text.Encoding.UTF8).GetBytes(text));

    public byte[] Compress(byte[] buffer)
    {
        using var ms = new MemoryStream();
        using var zipStream = new BrotliStream(ms, CompressionMode.Compress);
        zipStream.Write(buffer, 0, buffer.Length);
        zipStream.Close();

        return ms.ToArray();
    }

    public Stream Compress(Stream outputStream, bool leaveOpen=false) => 
        new BrotliStream(outputStream, CompressionMode.Compress, leaveOpen);

    public string Decompress(byte[] zipBuffer, Encoding? encoding = null)
    {
        using var uncompressedStream = MemoryStreamFactory.GetStream();
        using var compressedStream = MemoryStreamFactory.GetStream(zipBuffer);
        using var zipStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
        zipStream.CopyTo(uncompressedStream);
        return uncompressedStream.ReadToEnd(encoding ?? System.Text.Encoding.UTF8);
    }

    public Stream Decompress(Stream gzStream, bool leaveOpen=false) => 
        new BrotliStream(gzStream, CompressionMode.Decompress, leaveOpen);

    public byte[] DecompressBytes(byte[] zipBuffer)
    {
        using var compressedStream = zipBuffer.InMemoryStream();
        using var zipStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
        return zipStream.ReadFully();
    }
}

public class ZLibCompressor : IStreamCompressor
{
    public string Encoding => CompressionTypes.Deflate; 
    public static ZLibCompressor Instance { get; } = new();
        
    public byte[] Compress(string text, Encoding? encoding = null) => Compress((encoding ?? System.Text.Encoding.UTF8).GetBytes(text));

    public byte[] Compress(byte[] bytes)
    {
        // In .NET FX incompatible, you can't access compressed bytes without closing DeflateStream
        // Which means we must use MemoryStream since you have to use ToArray() on a closed Stream
        using var ms = new MemoryStream();
        using var zipStream = new ZLibStream(ms, CompressionMode.Compress);
        zipStream.Write(bytes, 0, bytes.Length);
        zipStream.Close();

        return ms.ToArray();
    }

    public Stream Compress(Stream outputStream, bool leaveOpen=false) => 
        new ZLibStream(outputStream, CompressionMode.Compress, leaveOpen);

    public string Decompress(byte[] zipBuffer, Encoding? encoding = null)
    {
        using var uncompressedStream = MemoryStreamFactory.GetStream();
        using var compressedStream = MemoryStreamFactory.GetStream(zipBuffer);
        using var zipStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
        zipStream.CopyTo(uncompressedStream);
        return uncompressedStream.ReadToEnd(encoding ?? System.Text.Encoding.UTF8);
    }

    public Stream Decompress(Stream zipBuffer, bool leaveOpen=false) => 
        new ZLibStream(zipBuffer, CompressionMode.Decompress, leaveOpen);

    public byte[] DecompressBytes(byte[] zipBuffer)
    {
        using var compressedStream = zipBuffer.InMemoryStream();
        using var zipStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
        return zipStream.ReadFully();
    }
}
#endif

public class DeflateCompressor : IStreamCompressor
{
    public string Encoding => CompressionTypes.Deflate; 
    public static DeflateCompressor Instance { get; } = new();
        
    public byte[] Compress(string text, Encoding? encoding = null) => Compress((encoding ?? System.Text.Encoding.UTF8).GetBytes(text));

    public byte[] Compress(byte[] bytes)
    {
        // In .NET FX incompatible, you can't access compressed bytes without closing DeflateStream
        // Which means we must use MemoryStream since you have to use ToArray() on a closed Stream
        using var ms = new MemoryStream();
        using var zipStream = new DeflateStream(ms, CompressionMode.Compress);
        zipStream.Write(bytes, 0, bytes.Length);
        zipStream.Close();

        return ms.ToArray();
    }

    public Stream Compress(Stream outputStream, bool leaveOpen=false) => 
        new DeflateStream(outputStream, CompressionMode.Compress, leaveOpen);

    public string Decompress(byte[] zipBuffer, Encoding? encoding = null)
    {
        using var uncompressedStream = MemoryStreamFactory.GetStream();
        using var compressedStream = MemoryStreamFactory.GetStream(zipBuffer);
        using var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        zipStream.CopyTo(uncompressedStream);
        return uncompressedStream.ReadToEnd(encoding ?? System.Text.Encoding.UTF8);
    }

    public Stream Decompress(Stream zipBuffer, bool leaveOpen=false) => 
        new DeflateStream(zipBuffer, CompressionMode.Decompress, leaveOpen);

    public byte[] DecompressBytes(byte[] zipBuffer)
    {
        using var compressedStream = zipBuffer.InMemoryStream();
        using var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        return zipStream.ReadFully();
    }
}

public class GZipCompressor : IStreamCompressor
{
    public string Encoding => CompressionTypes.GZip; 
    public static GZipCompressor Instance { get; } = new();
    public byte[] Compress(string text, Encoding? encoding = null) => Compress((encoding ?? System.Text.Encoding.UTF8).GetBytes(text));

    public byte[] Compress(byte[] buffer)
    {
        using var ms = new MemoryStream();
        using var zipStream = new GZipStream(ms, CompressionMode.Compress);
        zipStream.Write(buffer, 0, buffer.Length);
        zipStream.Close();

        return ms.ToArray();
    }

    public Stream Compress(Stream outputStream, bool leaveOpen=false) => 
        new GZipStream(outputStream, CompressionMode.Compress, leaveOpen);

    public string Decompress(byte[] gzBuffer, Encoding? encoding = null)
    {
        using var uncompressedStream = MemoryStreamFactory.GetStream();
        using var compressedStream = MemoryStreamFactory.GetStream(gzBuffer);
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        zipStream.CopyTo(uncompressedStream);
        return uncompressedStream.ReadToEnd(encoding ?? System.Text.Encoding.UTF8);
    }

    public byte[] DecompressBytes(byte[] gzBuffer)
    {
        using var compressedStream = gzBuffer.InMemoryStream();
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        return zipStream.ReadFully();
    }

    public Stream Decompress(Stream gzStream, bool leaveOpen=false) => 
        new GZipStream(gzStream, CompressionMode.Decompress, leaveOpen);
}
