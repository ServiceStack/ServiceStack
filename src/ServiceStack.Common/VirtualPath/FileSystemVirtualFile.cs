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

        public override DateTime LastModified => BackingFile.LastWriteTimeUtc;

        public override long Length => BackingFile.Length;

        public FileSystemVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory, FileInfo fInfo) 
            : base(owningProvider, directory)
        {
            this.BackingFile = fInfo ?? throw new ArgumentNullException(nameof(fInfo));
        }

        public override Stream OpenRead()
        {
            var i = 0;
            var firstAttempt = DateTime.UtcNow;
            IOException originalEx = null;
            
            while (DateTime.UtcNow - firstAttempt < VirtualPathUtils.MaxRetryOnExceptionTimeout)
            {
                try
                {
                    i++;
                    return BackingFile.OpenRead();
                }
                catch (IOException ex) // catch The process cannot access the file '...' because it is being used by another process.
                {
                    if (originalEx == null)
                        originalEx = ex;
                    
                    i.SleepBackOffMultiplier();
                }
            }
            
            throw new TimeoutException($"Exceeded timeout of {VirtualPathUtils.MaxRetryOnExceptionTimeout}", originalEx);
        }

        public override void Refresh()
        {
            BackingFile.Refresh();
        }
    }
}
