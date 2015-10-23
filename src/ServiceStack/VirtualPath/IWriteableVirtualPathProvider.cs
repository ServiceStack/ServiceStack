using System;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public interface IWriteableVirtualPathProvider
    {
        void AddFile(string filePath, string textContents);

        void AddFile(string filePath, Stream stream);
    }

    public static class WriteableVirtualPathProviderExtensions
    {
        public static void AddFile(this IVirtualPathProvider pathProvider, string filePath, string textContents)
        {
            var writable = pathProvider as IWriteableVirtualPathProvider;
            if (writable == null)
                throw new InvalidOperationException("{0} does not implement IWriteableVirtualPathProvider"
                    .Fmt(pathProvider.GetType().Name));

            writable.AddFile(filePath, textContents);
        }

        public static void AddFile(this IVirtualPathProvider pathProvider, string filePath, Stream stream)
        {
            var writable = pathProvider as IWriteableVirtualPathProvider;
            if (writable == null)
                throw new InvalidOperationException("{0} does not implement IWriteableVirtualPathProvider"
                    .Fmt(pathProvider.GetType().Name));

            writable.AddFile(filePath, stream);
        }
    }
}