using System;
using System.Collections.Generic;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualPathProviderBase : IVirtualPathProvider
    {
        public abstract IVirtualDirectory RootDirectory { get; }
        public abstract string VirtualPathSeparator { get; }
        public abstract string RealPathSeparator { get; }

        public virtual string CombineVirtualPath(string basePath, string relativePath)
        {
            return string.Concat(basePath, VirtualPathSeparator, relativePath);
        }

        public virtual bool FileExists(string virtualPath)
        {
            return GetFile(SanitizePath(virtualPath)) != null;
        }

        public virtual string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == '/' ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('\\', '/');
        }

        public virtual bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(SanitizePath(virtualPath)) != null;
        }

        public virtual IVirtualFile GetFile(string virtualPath)
        {
            var virtualFile = RootDirectory.GetFile(SanitizePath(virtualPath));
            virtualFile?.Refresh();
            return virtualFile;
        }

        public virtual string GetFileHash(string virtualPath)
        {
            var f = GetFile(virtualPath);
            return GetFileHash(f);
        }

        public virtual string GetFileHash(IVirtualFile virtualFile)
        {
            return virtualFile == null ? string.Empty : virtualFile.GetFileHash();
        }

        public virtual IVirtualDirectory GetDirectory(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath) || virtualPath == "/")
                return RootDirectory;
            
            return RootDirectory.GetDirectory(SanitizePath(virtualPath));
        }

        public virtual IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            return RootDirectory.GetAllMatchingFiles(globPattern, maxDepth);
        }

        public virtual IEnumerable<IVirtualFile> GetAllFiles()
        {
            return RootDirectory.GetAllMatchingFiles("*");
        }

        public virtual IEnumerable<IVirtualFile> GetRootFiles()
        {
            return RootDirectory.Files;
        }

        public virtual IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            return RootDirectory.Directories;
        }

        public virtual bool IsSharedFile(IVirtualFile virtualFile)
        {
            return virtualFile.RealPath != null
                && virtualFile.RealPath.Contains($"{RealPathSeparator}Shared");
        }

        public virtual bool IsViewFile(IVirtualFile virtualFile)
        {
            return virtualFile.RealPath != null
                && virtualFile.RealPath.Contains($"{RealPathSeparator}Views");
        }

        protected abstract void Initialize();

        public override string ToString() => $"[{GetType().Name}: {RootDirectory.RealPath}]";
    }
}