using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class FileSystemMapping : AbstractVirtualPathProviderBase
    {
        protected readonly DirectoryInfo RootDirInfo;
        protected FileSystemVirtualDirectory RootDir;
        public string Alias { get; private set; }

        public override IVirtualDirectory RootDirectory => RootDir;
        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

        public FileSystemMapping(string alias, string rootDirectoryPath)
            : this(alias, new DirectoryInfo(rootDirectoryPath))
        { }

        public FileSystemMapping(string alias, DirectoryInfo rootDirInfo)
        {
            if (alias == null)
                throw new ArgumentNullException(nameof(alias));

            if (alias.IndexOfAny(new []{ '/', '\\' }) >= 0)
                throw new ArgumentException($"Alias '{alias}' cannot contain directory separators");

            this.Alias = alias;
            this.RootDirInfo = rootDirInfo ?? throw new ArgumentNullException(nameof(rootDirInfo));
            Initialize();
        }

        protected sealed override void Initialize()
        {
            if (!RootDirInfo.Exists)
                throw new Exception($"RootDir '{RootDirInfo.FullName}' for virtual path does not exist");

            RootDir = new FileSystemVirtualDirectory(this, null, RootDirInfo);
        }

        public string GetRealVirtualPath(string virtualPath)
        {
            virtualPath = virtualPath.TrimStart('/');
            return virtualPath.StartsWith(Alias, StringComparison.OrdinalIgnoreCase)
                ? virtualPath.Substring(Alias.Length)
                : null;
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            var nodePath = GetRealVirtualPath(virtualPath);
            return !string.IsNullOrEmpty(nodePath)
                ? base.GetFile(nodePath)
                : null;
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            if (virtualPath.EqualsIgnoreCase(Alias))
                return RootDir;

            var nodePath = GetRealVirtualPath(virtualPath);
            return !string.IsNullOrEmpty(nodePath)
                ? base.GetDirectory(nodePath)
                : null;
        }

        public override IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            return new[] { new InMemoryVirtualDirectory(new MemoryVirtualFiles(), Alias), };
        }

        public override IEnumerable<IVirtualFile> GetRootFiles()
        {
            return new IVirtualFile[0];
        }
    }
}
