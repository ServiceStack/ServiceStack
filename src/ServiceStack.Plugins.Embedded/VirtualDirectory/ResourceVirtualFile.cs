using System;
using System.IO;
using System.Reflection;
using ServiceStack.VirtualPath;

namespace ServiceStack.Plugins.Embedded.VirtualPath
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

        public override DateTime LastModified
        {
            get { return GetLastWriteTimeOfBackingAsm(); }
        }

        public ResourceVirtualFile(IVirtualPathProvider owningProvider, ResourceVirtualDirectory directory,  string fileName)
            : base(owningProvider, directory)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");

            if (directory.BackingAssembly == null)
                throw new ArgumentException("parentDirectory");

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
