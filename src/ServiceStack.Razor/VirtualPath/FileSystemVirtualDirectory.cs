using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceStack.Razor.VirtualPath
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

            return directoryNodes.Union<IVirtualNode>(fileNodes)
                .GetEnumerator();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fName)
        {
            var fInfo = BackingDirInfo.EnumerateFiles(fName, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return fInfo != null
                ? new FileSystemVirtualFile(VirtualPathProvider, this, fInfo)
                : null;
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(String globPattern)
        {
            var matchingFilesInBackingDir = BackingDirInfo.EnumerateFiles(globPattern, SearchOption.TopDirectoryOnly)
                .Select(fInfo => new FileSystemVirtualFile(VirtualPathProvider, this, fInfo));
            
            return matchingFilesInBackingDir;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string dName)
        {
            var dInfo = BackingDirInfo.EnumerateDirectories(dName, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return dInfo != null
                ? new FileSystemVirtualDirectory(VirtualPathProvider, this, dInfo)
                : null;
        }
    }
}
