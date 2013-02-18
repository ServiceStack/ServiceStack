using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class FileSystemVirtualDirectory : AbstractVirtualDirectoryBase
    {
        protected DirectoryInfo BackingDirInfo;

        public override IEnumerable<IVirtualFile> Files
        {
            get { return this.Where(n => n.IsDirectory == false).Cast<IVirtualFile>(); }
        }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return this.Where(n => n.IsDirectory).Cast<IVirtualDirectory>(); }
        }

        public override string Name
        {
            get { return BackingDirInfo.Name; }
        }

        public override DateTime LastModified
        {
            get { return BackingDirInfo.LastWriteTime; }
        }

        public override string RealPath
        {
            get { return BackingDirInfo.FullName; }
        }

        public FileSystemVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory, DirectoryInfo dInfo)
            : base(owningProvider, parentDirectory)
        {
            if (dInfo == null)
                throw new ArgumentNullException("dInfo");

            this.BackingDirInfo = dInfo;
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            var directoryNodes = BackingDirInfo.GetDirectories()
                .Select(dInfo => new FileSystemVirtualDirectory(VirtualPathProvider, this, dInfo));

            var fileNodes = BackingDirInfo.GetFiles()
                .Select(fInfo => new FileSystemVirtualFile(VirtualPathProvider, this, fInfo));

            return directoryNodes.Cast<IVirtualNode>()
                .Union<IVirtualNode>(fileNodes.Cast<IVirtualNode>())
                .GetEnumerator();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fName)
        {
            var fInfo = EnumerateFiles(fName).FirstOrDefault();

            return fInfo != null
                ? new FileSystemVirtualFile(VirtualPathProvider, this, fInfo)
                : null;
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var matchingFilesInBackingDir = EnumerateFiles(globPattern)
                .Select(fInfo => (IVirtualFile)new FileSystemVirtualFile(VirtualPathProvider, this, fInfo));
            
            return matchingFilesInBackingDir;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string dName)
        {
            var dInfo = EnumerateDirectories(dName)
                .FirstOrDefault();

            return dInfo != null
                ? new FileSystemVirtualDirectory(VirtualPathProvider, this, dInfo)
                : null;
        }

        public IEnumerable<FileInfo> EnumerateFiles(string pattern)
        {
            return BackingDirInfo.GetFiles(pattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string dirName)
        {
            return BackingDirInfo.GetDirectories(dirName, SearchOption.TopDirectoryOnly);
        }
    }
}
