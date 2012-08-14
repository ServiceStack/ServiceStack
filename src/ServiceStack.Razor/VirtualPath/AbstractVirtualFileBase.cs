using System;
using System.IO;
using System.Security.Cryptography;

namespace ServiceStack.Razor.VirtualPath
{
    public abstract class AbstractVirtualFileBase : IVirtualFile
    {
        public IVirtualPathProvider VirtualPathProvider { get; set; }
        protected IVirtualDirectory ParentDirectory;

        public virtual bool IsDirectory { get { return false; } }
        public virtual string VirtualPath { get { return GetVirtualPathToRoot(); } }
        public virtual string RealPath { get { return GetRealPathToRoot(); } }

        public abstract string Name { get; }
        public abstract DateTime LastModified { get; }

        protected AbstractVirtualFileBase(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
        {
            if (owningProvider == null)
                throw new ArgumentNullException("owningProvider");

            if (parentDirectory == null)
                throw new ArgumentNullException("parentDirectory");

            this.VirtualPathProvider = owningProvider;
            this.ParentDirectory = parentDirectory;
        }

        public virtual string GetFileHash()
        {
            using (var stream = OpenRead())
            {
                return MD5.Create().ComputeHash(stream).ToString();
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
            return GetPathToRoot(VirtualPathProvider.VirtualPathSeparator, p => p.VirtualPath);
        }

        protected virtual string GetRealPathToRoot()
        {
            return GetPathToRoot(VirtualPathProvider.RealPathSeparator, p => p.RealPath);
        }

        protected virtual string GetPathToRoot(string separator, Func<IVirtualDirectory, string> pathSel)
        {
            var parentPath = ParentDirectory != null ? pathSel(ParentDirectory) : string.Empty;
            if (parentPath == separator)
                parentPath = string.Empty;

            return string.Concat(parentPath, separator, Name);
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
            return string.Format("{0} -> {1}", RealPath, VirtualPath);
        }
    }
}