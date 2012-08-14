using System;
using System.Collections.Generic;

namespace ServiceStack.Razor.VirtualPath
{
    public interface IVirtualDirectory : IVirtualNode, IEnumerable<IVirtualNode>
    {
        IEnumerable<IVirtualFile> Files { get; }
        IEnumerable<IVirtualDirectory> Directories { get; }
        bool IsRoot { get; }

        IVirtualFile GetFile(string virtualPath);
        IVirtualFile GetFile(Stack<string> virtualPath);

        IVirtualDirectory GetDirectory(string virtualPath);
        IVirtualDirectory GetDirectory(Stack<string> virtualPath);

        IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue);
    }
}
