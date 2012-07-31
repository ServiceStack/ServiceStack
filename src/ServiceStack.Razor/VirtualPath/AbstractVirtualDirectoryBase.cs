using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Razor.VirtualPath
{
    public abstract class AbstractVirtualDirectoryBase : IVirtualDirectory
    {
        #region Fields

        protected IVirtualPathProvider virtualPathProvider;
        protected IVirtualDirectory parentDirectory;

        #endregion

        protected AbstractVirtualDirectoryBase(IVirtualPathProvider owningProvider)
            : this(owningProvider, null)
        { }

        protected AbstractVirtualDirectoryBase(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
        {
            if (owningProvider == null)
                throw new ArgumentNullException("owningProvider");

            this.virtualPathProvider = owningProvider;
            this.parentDirectory = parentDirectory;
        }

        public virtual IVirtualFile GetFile(string virtualPath)
        {
            var tokens = virtualPath.TokenizeVirtualPath(virtualPathProvider);
            return GetFile(tokens);
        }

        public virtual IVirtualDirectory GetDirectory(string virtualPath)
        {
            var tokens = virtualPath.TokenizeVirtualPath(virtualPathProvider);
            return GetDirectory(tokens);
        }

        public virtual IVirtualFile GetFile(Stack<string> virtualPath)
        {
            if (virtualPath.Count == 0)
                return null;

            var pathToken = virtualPath.Pop();
            if (virtualPath.Count == 0)
                return GetFileFromBackingDirectoryOrDefault(pathToken);
            else
            {
                var virtDir = GetDirectoryFromBackingDirectoryOrDefault(pathToken);
                return virtDir != null
                        ? virtDir.GetFile(virtualPath)
                        : null;
            }
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

        protected virtual String GetVirtualPathToRoot()
        {
            if (IsRoot)
                return virtualPathProvider.VirtualPathSeparator;

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
            return String.Format("{0} -> {1}", RealPath, VirtualPath);
        }

        public abstract IEnumerator<IVirtualNode> GetEnumerator();

        protected abstract IVirtualFile GetFileFromBackingDirectoryOrDefault(String fileName);
        protected abstract IEnumerable<IVirtualFile> GetMatchingFilesInDir(String globPattern);
        protected abstract IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(String directoryName);

        #region Properties

        public virtual string VirtualPath { get { return GetVirtualPathToRoot(); } }
        public virtual string RealPath { get { return GetRealPathToRoot(); } }

        public virtual bool IsDirectory { get { return true; } }
        public virtual bool IsRoot { get { return parentDirectory == null; } }

        public abstract IEnumerable<IVirtualFile> Files { get; }
        public abstract IEnumerable<IVirtualDirectory> Directories { get; }

        public abstract string Name { get; }
        
        #endregion
    }
}