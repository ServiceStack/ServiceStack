using System;

namespace ServiceStack.VirtualPath
{
    public interface IVirtualNode
    {
        string Name { get; }
        string DirectoryName { get; }
        string VirtualPath { get; }
        string RealPath { get; }
        bool IsDirectory { get; }
        DateTime LastModified { get; }
    }
}
