using System;
using System.IO;
using System.Reflection;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class ResourceVirtualFile : AbstractVirtualFileBase
    {
        protected Assembly BackingAssembly;
        protected string FileName;
        
        public override string Name => FileName;

        public override string VirtualPath => GetVirtualPathToRoot();

        public override string RealPath => GetRealPathToRoot();

        public override DateTime LastModified => GetLastWriteTimeOfBackingAsm();

        private long? length;
        public override long Length
        {
            get
            {
                if (length == null)
                {
                    using (var s = OpenRead())
                    {
                        length = s.Length;
                    }
                }
                return length.Value;
            }
        }

        public ResourceVirtualFile(IVirtualPathProvider owningProvider, ResourceVirtualDirectory directory,  string fileName)
            : base(owningProvider, directory)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (directory.BackingAssembly == null)
                throw new ArgumentNullException("parentDirectory");

            this.FileName = fileName;
            this.BackingAssembly = directory.BackingAssembly;
        }

        public override Stream OpenRead()
        {
            var fullName = RealPath;
            return BackingAssembly.GetManifestResourceStream(fullName);
        }

        private DateTime GetLastWriteTimeOfBackingAsm()
        {
            var fInfo = new FileInfo(BackingAssembly.Location);
            return fInfo.LastWriteTime;
        }
    }
}
