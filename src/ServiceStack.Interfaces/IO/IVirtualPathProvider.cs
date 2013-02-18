using System;
using System.Collections.Generic;

namespace ServiceStack.IO
{
    public interface IVirtualPathProvider
    {
		IVirtualDirectory RootDirectory { get; }
        string VirtualPathSeparator { get; }
        string RealPathSeparator { get; }

        string CombineVirtualPath(string basePath, string relativePath);

        bool FileExists(string virtualPath);
        bool DirectoryExists(string virtualPath);

        IVirtualFile GetFile(string virtualPath);
        string GetFileHash(string virtualPath);
        string GetFileHash(IVirtualFile virtualFile);

        IVirtualDirectory GetDirectory(string virtualPath);

        IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue);

        bool IsSharedFile(IVirtualFile virtualFile);
        bool IsViewFile(IVirtualFile virtualFile);
    }
}
