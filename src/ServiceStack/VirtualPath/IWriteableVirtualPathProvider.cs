using System;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public interface IWriteableVirtualPathProvider
    {
        void WriteFile(string filePath, string textContents);

        void WriteFile(string filePath, Stream stream);
    }

    public static class WriteableVirtualPathProviderExtensions
    {
        [Obsolete("Renamed to WriteFile")]
        public static void AddFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            pathProvider.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException("{0} does not implement IWriteableVirtualPathProvider"
                    .Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, textContents);
        }

        public static void WriteFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
        {
            var writableFs = pathProvider as IWriteableVirtualPathProvider;
            if (writableFs == null)
                throw new InvalidOperationException("{0} does not implement IWriteableVirtualPathProvider"
                    .Fmt(pathProvider.GetType().Name));

            writableFs.WriteFile(filePath, stream);
        }
    }
}