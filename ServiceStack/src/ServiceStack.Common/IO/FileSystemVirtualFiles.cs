using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class FileSystemVirtualFiles
        : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public DirectoryInfo RootDirInfo { get; protected set; }
        protected FileSystemVirtualDirectory RootDir;

        public override IVirtualDirectory RootDirectory => RootDir;
        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

        public FileSystemVirtualFiles(string rootDirectoryPath)
            : this(new DirectoryInfo(rootDirectoryPath))
        {
        }

        public FileSystemVirtualFiles(DirectoryInfo rootDirInfo)
        {
            this.RootDirInfo = rootDirInfo ?? throw new ArgumentNullException(nameof(rootDirInfo));
            Initialize();
        }

        protected sealed override void Initialize()
        {
            if (!RootDirInfo.Exists)
                throw new Exception($"RootDir '{RootDirInfo.FullName}' for virtual path does not exist");

            RootDir = new FileSystemVirtualDirectory(this, null, RootDirInfo);
        }

        public override bool DirectoryExists(string virtualPath)
        {
            var isDirectory = Directory.Exists(RootDirectory.RealPath.CombineWith(SanitizePath(virtualPath)));
            return isDirectory;
        }

        public override bool FileExists(string virtualPath)
        {
            var isFile = File.Exists(RootDirectory.RealPath.CombineWith(SanitizePath(virtualPath)));
            return isFile;
        }

        public string EnsureDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            return dirPath;
        }

        public void WriteFile(string filePath, string textContents)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            File.WriteAllText(realFilePath, textContents);
        }

        public void WriteFile(string filePath, Stream stream)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            using var fs = File.Open(realFilePath, FileMode.Create, FileAccess.Write);
            stream.WriteTo(fs);
        }

        public override async Task WriteFileAsync(string filePath, object contents, CancellationToken token=default)
        {
            if (contents == null)
                return;

            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            using var fs = File.Open(realFilePath, FileMode.Create, FileAccess.Write);

            if (contents is IVirtualFile vfile)
                await WriteFileAsync(filePath, vfile.GetContents(), token).ConfigAwait();
            else if (contents is string textContents)
                await fs.WriteAsync(textContents, token).ConfigAwait();
            else if (contents is ReadOnlyMemory<char> romChars)
                await fs.WriteAsync(romChars.Span, token).ConfigAwait();
            else if (contents is byte[] binaryContents)
                await fs.WriteAsync(binaryContents, token: token).ConfigAwait();
            else if (contents is ReadOnlyMemory<byte> romBytes)
                await fs.WriteAsync(romBytes, token).ConfigAwait();
            else if (contents is Stream stream)
                await stream.CopyToAsync(fs, token).ConfigAwait();
            else
                throw CreateContentNotSupportedException(contents);
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void AppendFile(string filePath, string textContents)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            File.AppendAllText(realFilePath, textContents);
        }

        public void AppendFile(string filePath, Stream stream)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            using (var fs = new FileStream(realFilePath, FileMode.Append))
            {
                stream.WriteTo(fs);
            }
        }

        public void DeleteFile(string filePath)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            try
            {
                File.Delete(realFilePath);
            }
            catch (Exception /*ignore*/)
            {
            }
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            filePaths.Each(DeleteFile);
        }

        public void DeleteFolder(string dirPath)
        {
            var realPath = RootDir.RealPath.CombineWith(dirPath);
#if NETCORE
            // Doesn't properly recursively delete nested dirs/files on .NET Core (win at least)
            if (Directory.Exists(realPath))
                DeleteDirectoryRecursive(realPath);
#else
            if (Directory.Exists(realPath))
                Directory.Delete(realPath, recursive: true);
#endif
        }
        
        public static void DeleteDirectoryRecursive(string path)
        {
            //modified from https://stackoverflow.com/a/1703799/85785
            foreach (var directory in Directory.GetDirectories(path))
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                DeleteDirectoryRecursive(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException) 
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        public static string AssertDirectory(string dirPath, int timeoutMs=1000)
        {
            if (string.IsNullOrEmpty(dirPath))
                return null;

            try
            {
                ExecUtils.RetryOnException(() => {
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                }, TimeSpan.FromMilliseconds(timeoutMs));
                return dirPath;
            }
            catch (TimeoutException e)
            {
                throw e.InnerException ?? e;
            }
        }

        public static void RecreateDirectory(string dirPath, int timeoutMs = 1000)
        {
            if (Directory.Exists(dirPath))
            {
                DeleteDirectoryRecursive(dirPath);
            }
            AssertDirectory(dirPath, timeoutMs);
        }
        
    }
}
