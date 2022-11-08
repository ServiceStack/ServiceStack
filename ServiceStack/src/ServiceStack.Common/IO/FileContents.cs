#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.IO;

/// <summary>
/// Transforms multiple data content types into a FileContents containing either a binary Stream or text string
/// </summary>
public class FileContents
{
    public FileContents(Stream? stream) => Stream = stream;
    public FileContents(string? text) => Text = text;

    public Stream? Stream { get; }
    public string? Text { get; }

    /// <summary>
    /// Transform multi supported content types into FileContents containing either Stream or string.
    /// If returning unbuffered Stream responsibility is up to callee to properly dispose
    /// </summary>
    /// <param name="contents"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static async Task<FileContents?> GetAsync(object contents, bool buffer = false)
    {
        switch (contents)
        {
            case null:
                return new FileContents(string.Empty);
            case Stream stream when buffer:
            {
                var ms = await stream.CopyToNewMemoryStreamAsync().ConfigAwait();
                return new FileContents(ms);
            }
            case Stream stream:
                return new FileContents(stream);
            case string text:
                return new FileContents(text);
            case byte[] bytes:
                return new FileContents(MemoryStreamFactory.GetStream(bytes));
            case ReadOnlyMemory<char> romChars:
                return new FileContents(romChars.ToString());
            case ReadOnlyMemory<byte> romBytes:
            {
                var ms = MemoryStreamFactory.GetStream(romBytes.Length);
#if NET6_0_OR_GREATER
                ms.Write(romBytes.Span);
#else
                await MemoryProvider.Instance.WriteAsync(ms, romBytes).ConfigAwait();
#endif
                return new FileContents(ms);
            }
            case IVirtualFile virtualFile:
                return await GetAsync(virtualFile.GetContents(), buffer).ConfigAwait();
            case FileInfo fileInfo when buffer:
            {
                var ms = MemoryStreamFactory.GetStream((int)fileInfo.Length);
#if NET6_0_OR_GREATER
                await using var fs = fileInfo.OpenRead();
#else
                using var fs = fileInfo.OpenRead();
#endif
                await fs.CopyToAsync(ms).ConfigAwait();
                return new FileContents(fs);
            }
            case FileInfo fileInfo:
                return new FileContents(fileInfo.OpenRead());
            default:
                return null;
        }
    }
}
