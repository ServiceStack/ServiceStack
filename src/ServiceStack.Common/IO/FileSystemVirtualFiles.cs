using System.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class FileSystemVirtualFiles : FileSystemVirtualPathProvider
    {
        public FileSystemVirtualFiles(string rootDirectoryPath) : base(rootDirectoryPath) {}
        public FileSystemVirtualFiles(DirectoryInfo rootDirInfo) : base(rootDirInfo) {}
    }
}