using System;
using System.IO;

namespace ServiceStack.Razor.VirtualPath
{
    public class FileSystemVirtualFile : AbstractVirtualFileBase
    {
        protected FileInfo BackingFile;
        
        public override string Name
        {
            get { return BackingFile.Name; }
        }

        public override string RealPath
        {
            get { return BackingFile.FullName; }
        }

        public override DateTime LastModified
        {
            get { return BackingFile.LastWriteTime; }
        }

        public FileSystemVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory, FileInfo fInfo) 
            : base(owningProvider, parentDirectory)
        {
            if (fInfo == null)
                throw new ArgumentNullException("fInfo");

            this.BackingFile = fInfo;
        }

        public override Stream OpenRead()
        {
            return BackingFile.OpenRead();
        }
    }
}
