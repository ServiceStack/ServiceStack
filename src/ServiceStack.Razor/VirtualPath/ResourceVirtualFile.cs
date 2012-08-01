using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceStack.Razor.VirtualPath
{
    public class ResourceVirtualFile : AbstractVirtualFileBase
    {
        #region Fields

        protected Assembly backingAssembly;
        protected String fileName;

        #endregion

        public ResourceVirtualFile(IVirtualPathProvider owningProvider, ResourceVirtualDirectory parentDirectory,  String fileName)
            : base(owningProvider, parentDirectory)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");

            if (parentDirectory.BackingAssembly == null)
                throw new ArgumentException("parentDirectory");

            this.fileName = fileName;
            this.backingAssembly = parentDirectory.BackingAssembly;
        }

        public override Stream OpenRead()
        {
            var fullName = RealPath;
            return backingAssembly.GetManifestResourceStream(fullName);
        }

        private DateTime GetLastWriteTimeOfBackingAsm()
        {
            var fInfo = new FileInfo(backingAssembly.Location);
            return fInfo.LastWriteTime;
        }

        #region Properties

        public override string Name
        {
            get { return fileName; }
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

        #endregion

    }
}
