using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualFileBase : IVirtualFile
    {
        public static List<string> ScanSkipPaths { get; set; } = new List<string>();

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public virtual string Extension => Name.LastRightPart('.');

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
            using var stream = OpenRead();
            return stream.ToMd5Hash();
        }

        public virtual StreamReader OpenText()
        {
            return new StreamReader(OpenRead());
        }

        public virtual string ReadAllText()
        {
            using var reader = OpenText();
            var text = reader.ReadToEnd();
            return text;
        }

        public virtual byte[] ReadAllBytes()
        {
            var contents = GetContents();
            var bytes = contents is ReadOnlyMemory<byte> rom
                ? rom.ToArray()
                : contents is ReadOnlyMemory<char> romChars
                    ? MemoryProvider.Instance.ToUtf8(romChars.Span).ToArray()
                    : contents is string s
                        ? MemoryProvider.Instance.ToUtf8(s.AsSpan()).ToArray()
                        : ReadAllBytes();
            return bytes;
        }

        public virtual async Task<string> ReadAllTextAsync(CancellationToken token = default)
        {
            using var reader = OpenText();
            var text = await reader.ReadToEndAsync();
            return text;
        }

        public virtual Task<byte[]> ReadAllBytesAsync(CancellationToken token = default) => Task.FromResult(ReadAllBytes());

        public abstract Stream OpenRead();

        public virtual object GetContents()
        {
            using var stream = OpenRead();
            var romBytes = stream.ReadFullyAsMemory();
            if (MimeTypes.IsBinary(MimeTypes.GetMimeType(Extension)))
                return romBytes;

            return MemoryProvider.Instance.FromUtf8(romBytes.Span);
        }

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
            if (!(obj is AbstractVirtualFileBase other))
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

        public virtual async Task WritePartialToAsync(Stream toStream, long start, long end, CancellationToken token = default)
        {
            using var fs = OpenRead();
            await fs.WritePartialToAsync(toStream, start, end, token).ConfigAwait();
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

        public static IVirtualFile AssertFile(this IVirtualPathProvider vfs, string path)
        {
            if (vfs == null)
                throw new ArgumentNullException(nameof(vfs));
            var file = vfs.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"File does not exist {vfs.GetType().Name}:{path}");
            return file;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GistVirtualFiles GetGistVirtualFiles(this IVirtualPathProvider vfs) =>
            vfs.GetVirtualFileSource<GistVirtualFiles>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ResourceVirtualFiles GetResourceVirtualFiles(this IVirtualPathProvider vfs) =>
            vfs.GetVirtualFileSource<ResourceVirtualFiles>();

        [Obsolete("Use ReadAllTextAsMemory()")]
        public static ReadOnlyMemory<char> GetTextContentsAsMemory(this IVirtualFile file) => ReadAllTextAsMemory(file);

        public static ReadOnlyMemory<char> ReadAllTextAsMemory(this IVirtualFile file)
        {
            var contents = file.GetContents();
            var span = contents is ReadOnlyMemory<char> rom
                ? rom
                : contents is string s
                    ? s.AsMemory()
                    : file.ReadAllText().AsMemory();
            return span;
        }

        [Obsolete("Use ReadAllBytesAsMemory()")]
        public static ReadOnlyMemory<byte> GetBytesContentsAsMemory(this IVirtualFile file) => ReadAllBytesAsMemory(file);

        public static ReadOnlyMemory<byte> ReadAllBytesAsMemory(this IVirtualFile file)
        {
            var contents = file.GetContents();
            var span = contents is ReadOnlyMemory<byte> rom
                ? rom
                : contents is ReadOnlyMemory<char> romChars
                    ? MemoryProvider.Instance.ToUtf8(romChars.Span)
                    : contents is string s
                        ? MemoryProvider.Instance.ToUtf8(s.AsSpan())
                        : file.ReadAllBytes().AsMemory();
            return span;
        }

        [Obsolete("Use ReadAllBytes()")]
        public static byte[] GetBytesContentsAsBytes(this IVirtualFile file) => file.ReadAllBytes();
    }

}