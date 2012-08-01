using System;
using System.Collections.Generic;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualPathProvider
    {
        String CombineVirtualPath(String basePath, String relativePath);

        bool FileExists(String virtualPath);
        bool DirectoryExists(String virtualPath);

        IVirtualFile GetFile(String virtualPath);
        String GetFileHash(String virtualPath);
        String GetFileHash(IVirtualFile virtualFile);

        IVirtualDirectory GetDirectory(String virtualPath);

        IEnumerable<IVirtualFile> GetAllMatchingFiles(String globPattern, int maxDepth = Int32.MaxValue);

        bool IsSharedFile(IVirtualFile virtualFile);
        bool IsViewFile(IVirtualFile virtualFile);

        #region Properties

        IAppHost AppHost { get; }
        IVirtualDirectory RootDirectory { get; }
        
        String VirtualPathSeparator { get; }
        String RealPathSeparator { get; }

        #endregion
    }
}
