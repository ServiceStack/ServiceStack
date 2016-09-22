using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public static class VirtualPathProviderExtensions
    {
        private const string ErrorNotWritable = "{0} does not implement IVirtualFiles";

        public static bool IsFile(this IVirtualPathProvider pathProvider, string filePath)
        {
            return pathProvider.FileExists(filePath);
        }

        public static bool IsDirectory(this IVirtualPathProvider pathProvider, string filePath)
        {
            return pathProvider.DirectoryExists(filePath);
        }

        [Obsolete("Renamed to WriteFile")]
        public static void AddFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            pathProvider.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, stream);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                writableFs.WriteFile(filePath, ms);
            }
        }

        public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.AppendFile(filePath, textContents);
        }

        public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.AppendFile(filePath, stream);
        }

        public static void AppendFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                writableFs.AppendFile(filePath, ms);
            }
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, IVirtualFile file, string filePath = null)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            using (var stream = file.OpenRead())
            {
                writableFs.WriteFile(filePath ?? file.VirtualPath, stream);
            }
        }

        public static void DeleteFile(this IVirtualPathProvider pathProvider, string filePath)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFile(filePath);
        }

        public static void DeleteFile(this IVirtualPathProvider pathProvider, IVirtualFile file)
        {
            pathProvider.DeleteFile(file.VirtualPath);
        }

        public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<string> filePaths)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFiles(filePaths);
        }

        public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> files)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFiles(files.Map(x => x.VirtualPath));
        }

        public static void DeleteFolder(this IVirtualPathProvider pathProvider, string dirPath)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFolder(dirPath);
        }

        public static void WriteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath = null)
        {
            var writableFs = pathProvider as IVirtualFiles;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFiles(srcFiles, toPath);
        }

        public static void CopyFrom(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath=null)
        {
            foreach (var file in srcFiles)
            {
                using (var stream = file.OpenRead())
                {
                    var dstPath = toPath != null ? toPath(file) : file.VirtualPath;
                    if (dstPath == null)
                        continue;

                    pathProvider.WriteFile(dstPath, stream);
                }
            }
        }
    }

    public static class VirtualDirectoryExtensions
    {
        public static IEnumerable<IVirtualFile> GetFiles(this IVirtualDirectory dir)
        {
            return dir.Files;
        }

        public static IEnumerable<IVirtualDirectory> GetDirectories(this IVirtualDirectory dir)
        {
            return dir.Directories;
        }
    }
}