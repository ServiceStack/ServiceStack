using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceStack.IO
{
    public interface IVirtualFilesAsync : IVirtualFiles
    {
        Task WriteFileAsync(string filePath, string textContents);

        Task WriteFileAsync(string filePath, Stream stream);

        Task WriteFilesAsync(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null);
        
        Task AppendFileAsync(string filePath, string textContents);

        Task AppendFileAsync(string filePath, Stream stream);

        Task DeleteFileAsync(string filePath);

        Task DeleteFilesAsync(IEnumerable<string> filePaths);

        Task DeleteFolderAsync(string dirPath);
    }
}