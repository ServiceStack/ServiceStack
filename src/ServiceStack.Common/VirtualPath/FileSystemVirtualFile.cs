using System;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class FileSystemVirtualFile : AbstractVirtualFileBase
    {
        protected FileInfo BackingFile;
        
        public override string Name => BackingFile.Name;

        public override string RealPath => BackingFile.FullName;

        public override DateTime LastModified => BackingFile.LastWriteTime;

        public override long Length => BackingFile.Length;

        public FileSystemVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory, FileInfo fInfo) 
            : base(owningProvider, directory)
        {
            this.BackingFile = fInfo ?? throw new ArgumentNullException(nameof(fInfo));
        }

        public override Stream OpenRead()
        {
            return BackingFile.OpenRead();
        }

        public override void Refresh()
        {
            BackingFile.Refresh();
        }
    }
}
