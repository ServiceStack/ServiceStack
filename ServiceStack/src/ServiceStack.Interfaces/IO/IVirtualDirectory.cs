using System;
using System.Collections.Generic;

namespace ServiceStack.IO
{
    public interface IVirtualDirectory : IVirtualNode, IEnumerable<IVirtualNode>
    {
        bool IsRoot { get; }
        IVirtualDirectory ParentDirectory { get; }

        IEnumerable<IVirtualFile> Files { get; }
        IEnumerable<IVirtualDirectory> Directories { get; }

        IVirtualFile GetFile(string virtualPath);
        IVirtualFile GetFile(Stack<string> virtualPath);

        IVirtualDirectory GetDirectory(string virtualPath);
        IVirtualDirectory GetDirectory(Stack<string> virtualPath);

        IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue);
    }
}
