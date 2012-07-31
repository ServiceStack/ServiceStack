using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceStack.Razor.VirtualPath
{
    public class FileSystemVirtualDirectory : AbstractVirtualDirectoryBase
    {
        #region Fields

        protected DirectoryInfo backingDirInfo;

        #endregion

        public FileSystemVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory, DirectoryInfo dInfo)
            : base(owningProvider, parentDirectory)
        {
            if (dInfo == null)
                throw new ArgumentNullException("dInfo");

            this.backingDirInfo = dInfo;
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            var directoryNodes = backingDirInfo.GetDirectories()
                                               .Select(dInfo => new FileSystemVirtualDirectory(virtualPathProvider, this, dInfo));

            var fileNodes = backingDirInfo.GetFiles()
                                          .Select(fInfo => new FileSystemVirtualFile(virtualPathProvider, this, fInfo));

            return Enumerable.Union<IVirtualNode>(directoryNodes, fileNodes)
                             .GetEnumerator();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fName)
        {
            var fInfo = backingDirInfo.EnumerateFiles(fName, SearchOption.TopDirectoryOnly)
                                      .FirstOrDefault();

            return fInfo != null
                ? new FileSystemVirtualFile(virtualPathProvider, this, fInfo)
                : null;
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(String globPattern)
        {
            var matchingFilesInBackingDir = backingDirInfo.EnumerateFiles(globPattern, SearchOption.TopDirectoryOnly)
                                                          .Select(fInfo => new FileSystemVirtualFile(virtualPathProvider, this, fInfo));
            return matchingFilesInBackingDir;
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string dName)
        {
            var dInfo = backingDirInfo.EnumerateDirectories(dName, SearchOption.TopDirectoryOnly)
                                      .FirstOrDefault();

            return dInfo != null
                ? new FileSystemVirtualDirectory(virtualPathProvider, this, dInfo)
                : null;
        }

        #region Properties

        public override IEnumerable<IVirtualFile> Files
        {
            get { return this.Where(n => n.IsDirectory == false).Cast<IVirtualFile>(); }
        }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return this.Where(n => n.IsDirectory == true).Cast<IVirtualDirectory>(); }
        }

        public override string Name
        {
            get { return backingDirInfo.Name; }
        }

        public override string RealPath
        {
            get { return backingDirInfo.FullName; }
        }

        #endregion



        
    }
}
