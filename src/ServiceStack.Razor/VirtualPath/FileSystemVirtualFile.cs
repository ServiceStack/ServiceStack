using System;
using System.IO;

namespace ServiceStack.Razor.VirtualPath
{
    public class FileSystemVirtualFile : AbstractVirtualFileBase
    {
        #region Fields

        protected FileInfo backingFile;

        #endregion

        public FileSystemVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory, FileInfo fInfo) 
            : base(owningProvider, parentDirectory)
        {
            if (fInfo == null)
                throw new ArgumentNullException("fInfo");

            this.backingFile = fInfo;
        }

        public override Stream OpenRead()
        {
            return backingFile.OpenRead();
        }

        
        #region Properties

        public override string Name
        {
            get { return backingFile.Name; }
        }

        public override string RealPath
        {
            get { return backingFile.FullName; }
        }

        public override DateTime LastModified
        {
            get { return backingFile.LastWriteTime; }
        }

        #endregion


    }
}
