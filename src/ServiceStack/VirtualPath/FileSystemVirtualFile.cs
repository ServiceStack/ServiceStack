using System;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
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

        public FileSystemVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory, FileInfo fInfo) 
            : base(owningProvider, directory)
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
