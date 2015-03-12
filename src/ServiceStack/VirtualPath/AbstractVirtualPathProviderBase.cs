using System;
using System.Collections.Generic;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualPathProviderBase : IVirtualPathProvider
    {
        public IAppHost AppHost { get; protected set; }
        public abstract IVirtualDirectory RootDirectory { get; }
        public abstract string VirtualPathSeparator { get; }
        public abstract string RealPathSeparator { get; }

        protected AbstractVirtualPathProviderBase(IAppHost appHost)
        {
            if (appHost == null)
                throw new ArgumentNullException("appHost");

            AppHost = appHost;
        }

        public virtual string CombineVirtualPath(string basePath, string relativePath)
        {
            return String.Concat(basePath, VirtualPathSeparator, relativePath);
        }

        public virtual bool FileExists(string virtualPath)
        {
            return GetFile(virtualPath) != null;
        }

        public virtual bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(virtualPath) != null;
        }

        public virtual IVirtualFile GetFile(string virtualPath)
        {
            return RootDirectory.GetFile(virtualPath).Refresh();
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
            return RootDirectory.GetDirectory(virtualPath);
        }

        public virtual IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            return RootDirectory.GetAllMatchingFiles(globPattern, maxDepth);
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
                && virtualFile.RealPath.Contains("{0}{1}".Fmt(RealPathSeparator, "Shared"));
        }

        public virtual bool IsViewFile(IVirtualFile virtualFile)
        {
            return virtualFile.RealPath != null
                && virtualFile.RealPath.Contains("{0}{1}".Fmt(RealPathSeparator, "Views"));
        }

        protected abstract void Initialize();

        public override string ToString()
        {
            return "[{0}: {1}]".Fmt(GetType().Name, RootDirectory.RealPath);
        }
    }
}