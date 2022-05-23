using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public abstract class AbstractVirtualPathProviderBase : IVirtualPathProvider
    {
        public abstract IVirtualDirectory RootDirectory { get; }
        public abstract string VirtualPathSeparator { get; }
        public abstract string RealPathSeparator { get; }

        public virtual string CombineVirtualPath(string basePath, string relativePath)
        {
            return string.Concat(basePath, VirtualPathSeparator, relativePath);
        }

        public virtual bool FileExists(string virtualPath)
        {
            return GetFile(SanitizePath(virtualPath)) != null;
        }

        public virtual string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == '/' ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('\\', '/');
        }

        public virtual bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(SanitizePath(virtualPath)) != null;
        }

        public virtual IVirtualFile GetFile(string virtualPath)
        {
            var virtualFile = RootDirectory.GetFile(SanitizePath(virtualPath));
            virtualFile?.Refresh();
            return virtualFile;
        }

        public virtual string GetFileHash(string virtualPath)
        {
            var f = GetFile(virtualPath);
            return GetFileHash(f);
        }

        public virtual string GetFileHash(IVirtualFile virtualFile)
        {
            return virtualFile == null ? string.Empty : virtualFile.GetFileHash();
        }

        public virtual IVirtualDirectory GetDirectory(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath) || virtualPath == "/")
                return RootDirectory;
            
            return RootDirectory.GetDirectory(SanitizePath(virtualPath));
        }

        public virtual IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
        {
            return RootDirectory.GetAllMatchingFiles(globPattern, maxDepth);
        }

        public virtual IEnumerable<IVirtualFile> GetAllFiles()
        {
            return RootDirectory.GetAllMatchingFiles("*");
        }

        public virtual IEnumerable<IVirtualFile> GetRootFiles()
        {
            return RootDirectory.Files;
        }

        public virtual IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            return RootDirectory.Directories;
        }

        public virtual bool IsSharedFile(IVirtualFile virtualFile)
        {
            return virtualFile.RealPath != null
                && virtualFile.RealPath.Contains($"{RealPathSeparator}Shared");
        }

        public virtual bool IsViewFile(IVirtualFile virtualFile)
        {
            return virtualFile.RealPath != null
                && virtualFile.RealPath.Contains($"{RealPathSeparator}Views");
        }

        protected abstract void Initialize();

        public override string ToString() => $"[{GetType().Name}: {RootDirectory.RealPath}]";

        public virtual void WriteFiles(Dictionary<string, string> textFiles)
        {
            var vfs = this as IVirtualFiles;
            if (vfs == null)
                throw new NotSupportedException($"{GetType().Name} does not implement IVirtualFiles");
            
            foreach (var entry in textFiles)
            {
                vfs.WriteFile(entry.Key, entry.Value);
            }
        }
        
        protected NotSupportedException CreateContentNotSupportedException(object value) =>
            new($"Could not write '{value?.GetType().Name ?? "null"}' value. Only string, byte[], Stream or IVirtualFile content is supported.");

        protected IVirtualFiles AssertVirtualFiles()
        {
            if (this is not IVirtualFiles vfs)
                throw new NotSupportedException($"{GetType().Name} does not implement IVirtualFiles");
            return vfs;
        }

        public virtual void WriteFile(string path, ReadOnlyMemory<char> text) => AssertVirtualFiles().WriteFile(path, text.ToString());

        public virtual void WriteFile(string path, ReadOnlyMemory<byte> bytes) => AssertVirtualFiles().WriteFile(path, ToMemoryStream(bytes));

        private static MemoryStream ToMemoryStream(ReadOnlyMemory<byte> bytes)
        {
            var ms = MemoryProvider.Instance.ToMemoryStream(bytes.Span);
            ms.Position = 0;
            return ms;
        }

        public virtual void WriteFile(string path, object contents)
        {
            if (contents == null)
                return;

            var vfs = AssertVirtualFiles();
            if (contents is IVirtualFile vfile)
                WriteFile(path, vfile.GetContents());
            else if (contents is string textContents)
                vfs.WriteFile(path, textContents);
            else if (contents is ReadOnlyMemory<char> romChars)
                WriteFile(path, romChars);
            else if (contents is byte[] binaryContents)
            {
                using var ms = MemoryStreamFactory.GetStream(binaryContents);
                vfs.WriteFile(path, ms);
            }
            else if (contents is ReadOnlyMemory<byte> romBytes)
                WriteFile(path, romBytes);
            else if (contents is Stream stream)
                vfs.WriteFile(path, stream);
            else
                throw CreateContentNotSupportedException(contents);
        }

        // Can implement all async APIs here
        public virtual Task WriteFileAsync(string path, object contents, CancellationToken token=default)
        {
            WriteFile(path, contents);
            return TypeConstants.EmptyTask;
        }
        
        public virtual void AppendFile(string path, ReadOnlyMemory<char> text) => AssertVirtualFiles().AppendFile(path, text.ToString());

        public virtual void AppendFile(string path, ReadOnlyMemory<byte> bytes) => AssertVirtualFiles().AppendFile(path, ToMemoryStream(bytes));

        public virtual void AppendFile(string path, object contents)
        {
            var vfs = AssertVirtualFiles();
            if (contents == null)
                return;

            if (contents is IVirtualFile vfile)
                AppendFile(path, vfile.GetContents());
            else if (contents is string textContents)
                vfs.AppendFile(path, textContents);
            else if (contents is ReadOnlyMemory<char> romChars)
                AppendFile(path, romChars);
            else if (contents is byte[] binaryContents)
            {
                using var ms = MemoryStreamFactory.GetStream(binaryContents);
                vfs.AppendFile(path, ms);
            }
            else if (contents is ReadOnlyMemory<byte> romBytes)
                AppendFile(path, romBytes);
            else if (contents is Stream stream)
                vfs.AppendFile(path, stream);
            else
                throw CreateContentNotSupportedException(contents);
        }

        public virtual void WriteFiles(Dictionary<string, object> files)
        {
            foreach (var entry in files)
            {
                WriteFile(entry.Key, entry.Value);
            }
        }
    }
}