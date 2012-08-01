using System;
using System.IO;
using System.Security.Cryptography;

namespace ServiceStack.Razor.VirtualPath
{
    public abstract class AbstractVirtualFileBase : IVirtualFile
    {
        #region Fields

        protected IVirtualPathProvider virtualPathProvider;
        protected IVirtualDirectory parentDirectory;

        #endregion

        protected AbstractVirtualFileBase(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
        {
            if (owningProvider == null)
                throw new ArgumentNullException("owningProvider");

            if (parentDirectory == null)
                throw new ArgumentNullException("parentDirectory");

            this.virtualPathProvider = owningProvider;
            this.parentDirectory = parentDirectory;
        }

        public virtual String GetFileHash()
        {
            using (var stream = OpenRead())
            {
                return MD5.Create().ComputeHash(stream)
                                   .ToString();
            }
        }

        public virtual StreamReader OpenText()
        {
            return new StreamReader(OpenRead());
        }

        public virtual string ReadAllText()
        {
            using (var reader = OpenText())
            {
                return reader.ReadToEnd();
            }
        }

        public abstract Stream OpenRead();

        protected virtual String GetVirtualPathToRoot()
        {
            return GetPathToRoot(virtualPathProvider.VirtualPathSeparator, p => p.VirtualPath);
        }

        protected virtual String GetRealPathToRoot()
        {
            return GetPathToRoot(virtualPathProvider.RealPathSeparator, p => p.RealPath);
        }

        protected virtual String GetPathToRoot(String separator, Func<IVirtualDirectory, String> pathSel)
        {
            var parentPath = parentDirectory != null ? pathSel(parentDirectory) : String.Empty;
            if (parentPath == separator)
                parentPath = String.Empty;

            return String.Concat(parentPath, separator, Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as AbstractVirtualFileBase;
            if (other == null)
                return false;

            return other.VirtualPath == this.VirtualPath;
        }

        public override int GetHashCode()
        {
            return VirtualPath.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0} -> {1}", RealPath, VirtualPath);
        }


        #region Properties

        public virtual bool IsDirectory { get { return false; } }
        public virtual string VirtualPath { get { return GetVirtualPathToRoot(); } }
        public virtual string RealPath { get { return GetRealPathToRoot(); } }

        public abstract string Name { get; }
        public abstract DateTime LastModified { get; }

        #endregion
    }
}