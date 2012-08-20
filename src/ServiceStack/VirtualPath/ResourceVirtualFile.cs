using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public class ResourceVirtualFile : AbstractVirtualFileBase
    {
        protected Assembly BackingAssembly;
        protected string FileName;
        
        public override string Name
        {
            get { return FileName; }
        }

        public override string VirtualPath
        {
            get { return GetVirtualPathToRoot(); }
        }

        public override string RealPath
        {
            get { return GetRealPathToRoot(); }
        }

        public override string DirectoryName
        {
            get { return VirtualPath.SplitOnLast(VirtualPathProvider.VirtualPathSeparator).First(); }
        }

        public override DateTime LastModified
        {
            get { return GetLastWriteTimeOfBackingAsm(); }
        }

        public ResourceVirtualFile(IVirtualPathProvider owningProvider, ResourceVirtualDirectory parentDirectory,  string fileName)
            : base(owningProvider, parentDirectory)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");

            if (parentDirectory.BackingAssembly == null)
                throw new ArgumentException("parentDirectory");

            this.FileName = fileName;
            this.BackingAssembly = parentDirectory.BackingAssembly;
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
