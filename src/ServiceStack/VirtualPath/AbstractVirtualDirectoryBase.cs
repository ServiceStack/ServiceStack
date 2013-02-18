using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualDirectoryBase : IVirtualDirectory
    {
        protected IVirtualPathProvider VirtualPathProvider;
        public IVirtualDirectory ParentDirectory { get; set; }
        public IVirtualDirectory Directory { get { return this; } }

        public abstract DateTime LastModified { get; }
        public virtual string VirtualPath { get { return GetVirtualPathToRoot(); } }
        public virtual string RealPath { get { return GetRealPathToRoot(); } }

        public virtual bool IsDirectory { get { return true; } }
        public virtual bool IsRoot { get { return ParentDirectory == null; } }

        public abstract IEnumerable<IVirtualFile> Files { get; }
        public abstract IEnumerable<IVirtualDirectory> Directories { get; }

        public abstract string Name { get; }
        
        protected AbstractVirtualDirectoryBase(IVirtualPathProvider owningProvider)
            : this(owningProvider, null) {}

        protected AbstractVirtualDirectoryBase(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
        {
            if (owningProvider == null)
                throw new ArgumentNullException("owningProvider");

            this.VirtualPathProvider = owningProvider;
            this.ParentDirectory = parentDirectory;
        }

        public virtual IVirtualFile GetFile(string virtualPath)
        {
            var tokens = virtualPath.TokenizeVirtualPath(VirtualPathProvider);
            return GetFile(tokens);
        }

        public virtual IVirtualDirectory GetDirectory(string virtualPath)
        {
            var tokens = virtualPath.TokenizeVirtualPath(VirtualPathProvider);
            return GetDirectory(tokens);
        }

        public virtual IVirtualFile GetFile(Stack<string> virtualPath)
        {
            if (virtualPath.Count == 0)
                return null;

            var pathToken = virtualPath.Pop();
            if (virtualPath.Count == 0)
                return GetFileFromBackingDirectoryOrDefault(pathToken);
            
            var virtDir = GetDirectoryFromBackingDirectoryOrDefault(pathToken);
            return virtDir != null
                   ? virtDir.GetFile(virtualPath)
                   : null;
        }

        public virtual IVirtualDirectory GetDirectory(Stack<string> virtualPath)
        {
            if (virtualPath.Count == 0)
                return null;

            var pathToken = virtualPath.Pop();

            var virtDir = GetDirectoryFromBackingDirectoryOrDefault(pathToken);
            if (virtDir == null)
                return null;

            return virtualPath.Count == 0
                ? virtDir
                : virtDir.GetDirectory(virtualPath);
        }

        public virtual IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            if (maxDepth == 0)
                yield break;

            foreach (var f in GetMatchingFilesInDir(globPattern))
                yield return f;

            foreach (var childDir in Directories)
            {
                var matchingFilesInChildDir = childDir.GetAllMatchingFiles(globPattern, maxDepth - 1);
                foreach (var f in matchingFilesInChildDir)
                    yield return f;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual string GetVirtualPathToRoot()
        {
            if (IsRoot)
                return VirtualPathProvider.VirtualPathSeparator;

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
            var other = obj as AbstractVirtualDirectoryBase;
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

        public abstract IEnumerator<IVirtualNode> GetEnumerator();

        protected abstract IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName);
        protected abstract IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern);
        protected abstract IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName);
    }
}