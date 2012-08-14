using System;
using System.IO;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualFile : IVirtualNode
    {
        IVirtualPathProvider VirtualPathProvider { get; }

        DateTime LastModified { get; }

        string GetFileHash();

        Stream OpenRead();
        StreamReader OpenText();
        string ReadAllText();
    }
}
