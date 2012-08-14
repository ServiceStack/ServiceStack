using System;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualNode
    {
        string Name { get; }
        string VirtualPath { get; }
        string RealPath { get; }
        bool IsDirectory { get; }
    }
}
