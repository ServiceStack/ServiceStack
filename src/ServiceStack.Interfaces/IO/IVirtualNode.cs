using System;

namespace ServiceStack.IO
{
    public interface IVirtualNode
    {
        IVirtualDirectory Directory { get; }
        string Name { get; }
        string VirtualPath { get; }
        string RealPath { get; }
        bool IsDirectory { get; }
        DateTime LastModified { get; }
    }
}
