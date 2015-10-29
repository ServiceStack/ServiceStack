using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public interface IWriteableVirtualPathProvider : IVirtualPathProvider
    {
        void WriteFile(string filePath, string textContents);

        void WriteFile(string filePath, Stream stream);

        void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null);

        void DeleteFile(string filePath);

        void DeleteFiles(IEnumerable<string> filePaths);

        void DeleteFolder(string dirPath);
    }

    public static class WriteableVirtualPathProviderExtensions
    {
        private const string ErrorNotWritable = "{0} does not implement IWriteableVirtualPathProvider";

        [Obsolete("Renamed to WriteFile")]
        public static void AddFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            pathProvider.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, stream);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, byte[] bytes)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                writableFs.WriteFile(filePath, ms);
            }
        }

        public static void DeleteFile(this IVirtualPathProvider pathProvider, string filePath)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFile(filePath);
        }

        public static void DeleteFiles(this IVirtualPathProvider pathProvider, IEnumerable<string> filePaths)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFiles(filePaths);
        }

        public static void DeleteFolder(this IVirtualPathProvider pathProvider, string dirPath)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.DeleteFolder(dirPath);
        }

        public static void WriteFiles(this IVirtualPathProvider pathProvider, IEnumerable<IVirtualFile> srcFiles, Func<IVirtualFile, string> toPath = null)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException(ErrorNotWritable.Fmt(pathProvider.GetType().Name));

            writableFs.WriteFiles(srcFiles);
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
}