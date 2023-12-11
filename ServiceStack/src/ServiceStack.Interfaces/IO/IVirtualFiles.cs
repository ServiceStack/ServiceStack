using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.IO;

public interface IVirtualFiles : IVirtualPathProvider
{
    Task WriteFileAsync(string filePath, object contents, CancellationToken token=default);

    void WriteFile(string filePath, string textContents);

    void WriteFile(string filePath, Stream stream);

    /// <summary>
    /// Contents can be either:
    /// string, ReadOnlyMemory&lt;char&gt;, byte[], `ReadOnlyMemory&lt;byte&gt;, Stream or IVirtualFile 
    /// </summary>
    void WriteFile(string filePath, object contents);

    void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null);

    void WriteFiles(Dictionary<string, string> textFiles);
    void WriteFiles(Dictionary<string, object> files);

    void AppendFile(string filePath, string textContents);

    void AppendFile(string filePath, Stream stream);

    /// <summary>
    /// Contents can be either:
    /// string, ReadOnlyMemory&lt;char&gt;, byte[], `ReadOnlyMemory&lt;byte&gt;, Stream or IVirtualFile 
    /// </summary>
    void AppendFile(string filePath, object contents);

    void DeleteFile(string filePath);

    void DeleteFiles(IEnumerable<string> filePaths);

    void DeleteFolder(string dirPath);
}