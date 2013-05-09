using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Common;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualFileBase : IVirtualFile
    {
        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public string Extension
        {
            get { return Name.SplitOnLast('.').LastOrDefault(); }
        }

        public IVirtualDirectory Directory { get; set; }

        public abstract string Name { get; }
        public virtual string VirtualPath { get { return GetVirtualPathToRoot(); } }
        public virtual string RealPath { get { return GetRealPathToRoot(); } }
        public virtual bool IsDirectory { get { return false; } }
        public abstract DateTime LastModified { get; }

        protected AbstractVirtualFileBase(
            IVirtualPathProvider owningProvider, IVirtualDirectory directory)
        {
            if (owningProvider == null)
                throw new ArgumentNullException("owningProvider");

            if (directory == null)
                throw new ArgumentNullException("directory");

            this.VirtualPathProvider = owningProvider;
            this.Directory = directory;
        }

        public virtual string GetFileHash()
        {
            using (var stream = OpenRead())
            {
                return stream.ToMd5Hash();
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
                var text = reader.ReadToEnd();
				return text;
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
            var parentPath = Directory != null ? pathSel(Directory) : string.Empty;
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