using System.IO;

namespace ServiceStack.VirtualPath
{
    public interface IVirtualFile : IVirtualNode
    {
        IVirtualPathProvider VirtualPathProvider { get; }

        string GetFileHash();

        Stream OpenRead();
        StreamReader OpenText();
        string ReadAllText();
    }
}
