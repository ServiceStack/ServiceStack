using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualFileBase : IVirtualFile
    {
        public static List<string> ScanSkipPaths { get; set; } = new List<string>();

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public string Extension => Name.LastRightPart('.');

        public IVirtualDirectory Directory { get; set; }

        public abstract string Name { get; }
        public virtual string VirtualPath => GetVirtualPathToRoot();
        public virtual string RealPath => GetRealPathToRoot();
        public virtual bool IsDirectory => false;
        public abstract DateTime LastModified { get; }
        public abstract long Length { get; }

        protected AbstractVirtualFileBase(IVirtualPathProvider owningProvider, IVirtualDirectory directory)
        {
            this.VirtualPathProvider = owningProvider ?? throw new ArgumentNullException(nameof(owningProvider));
            this.Directory = directory ?? throw new ArgumentNullException(nameof(directory));
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

        public virtual byte[] ReadAllBytes()
        {
            using (var stream = OpenRead())
            {
                return stream.ReadFully();
            }
        }

        public abstract Stream OpenRead();

        protected virtual string GetVirtualPathToRoot()
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

            return parentPath == null
                ? Name
                : string.Concat(parentPath, separator, Name);
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
            return $"{RealPath} -> {VirtualPath}";
        }

        public virtual void Refresh()
        {            
        }
    }
}

namespace ServiceStack
{
    public static class VirtualFileExtensions
    {
        public static bool ShouldSkipPath(this IVirtualNode node)
        {
            foreach (var skipPath in AbstractVirtualFileBase.ScanSkipPaths)
            {
                if (node.VirtualPath.StartsWith(skipPath.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        
        public static IVirtualDirectory[] GetAllRootDirectories(this IVirtualPathProvider vfs) => vfs is MultiVirtualFiles mvfs
            ? mvfs.ChildProviders.Select(x => x.RootDirectory).ToArray()
            : new[] { vfs.RootDirectory };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetVirtualFileSource<T>(this IVirtualPathProvider vfs) where T : class => vfs as T ??
            (vfs is MultiVirtualFiles mvfs ? mvfs.ChildProviders.FirstOrDefault(x => x is T) as T : null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryVirtualFiles GetMemoryVirtualFiles(this IVirtualPathProvider vfs) =>
            vfs.GetVirtualFileSource<MemoryVirtualFiles>();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileSystemVirtualFiles GetFileSystemVirtualFiles(this IVirtualPathProvider vfs) =>
            vfs.GetVirtualFileSource<FileSystemVirtualFiles>();
    }
    
}